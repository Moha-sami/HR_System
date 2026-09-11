using System.Data;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.Features.ShiftTemplates;
using Buy2.Application.Features.Sites.GetSiteShiftTemplates;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Schedules.SaveAsTemplate;

public class SaveAsTemplateCommandHandler
    : IRequestHandler<SaveAsTemplateCommand, Result<SiteShiftTemplateDto>>
{
    private const string DuplicateNameMessage =
        "A shift template with the same name already exists for this site.";

    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<ShiftTemplate> _shiftTemplateRepository;
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveAsTemplateCommandHandler(
        IRepository<Site> siteRepository,
        IRepository<ShiftTemplate> shiftTemplateRepository,
        IRepository<ShiftEntity> shiftRepository,
        IUnitOfWork unitOfWork)
    {
        _siteRepository = siteRepository;
        _shiftTemplateRepository = shiftTemplateRepository;
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SiteShiftTemplateDto>> Handle(
        SaveAsTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var shape = await new SaveAsTemplateValidator().ValidateAsync(request, cancellationToken);
        if (!shape.IsValid)
        {
            return Result<SiteShiftTemplateDto>.ValidationFailure(
                string.Join("; ", shape.Errors.Select(e => e.ErrorMessage)));
        }

        var name = request.Name!.Trim();

        var siteExists = await _siteRepository
            .Query()
            .AsNoTracking()
            .AnyAsync(s => s.Id == request.SiteId, cancellationToken);
        if (!siteExists)
        {
            return Result<SiteShiftTemplateDto>.NotFound(
                $"Site with ID {request.SiteId} was not found.");
        }

        // Fast-fail pre-check; the authoritative check runs inside the
        // serializable transaction in PersistAsync (closes the check-then-create race).
        if (await IsDuplicateNameAsync(request.SiteId, name, cancellationToken))
        {
            return Result<SiteShiftTemplateDto>.ValidationFailure(DuplicateNameMessage);
        }

        var shifts = await LoadDayShiftsAsync(request.SiteId, request.Date, cancellationToken);
        if (shifts.Count == 0)
        {
            return Result<SiteShiftTemplateDto>.ValidationFailure(
                "No shifts found for the requested date. Cannot create a template from an empty day.");
        }

        var (templateStart, templateEnd) = ComputeTemplateRange(shifts);

        return await TryPersistAsync(request, name, shifts, templateStart, templateEnd, cancellationToken);
    }

    private async Task<Result<SiteShiftTemplateDto>> TryPersistAsync(
        SaveAsTemplateCommand request,
        string name,
        List<ShiftEntity> shifts,
        TimeSpan templateStart,
        TimeSpan templateEnd,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await PersistAsync(request, name, shifts, templateStart, templateEnd, cancellationToken);
            if (!created.IsSuccess)
            {
                return Result<SiteShiftTemplateDto>.ValidationFailure(
                    created.ErrorMessage ?? DuplicateNameMessage);
            }

            return Result<SiteShiftTemplateDto>.Success(SiteShiftTemplateMapper.ToDto(created.Value!));
        }
        catch (DbUpdateException)
        {
            // Covers the concurrent-insert race (e.g. deadlock victim under
            // serializable isolation): re-check instead of trusting the exception.
            if (await IsDuplicateNameAsync(request.SiteId, name, CancellationToken.None))
            {
                return Result<SiteShiftTemplateDto>.ValidationFailure(DuplicateNameMessage);
            }

            throw;
        }
    }

    private async Task<bool> IsDuplicateNameAsync(
        int siteId,
        string name,
        CancellationToken cancellationToken)
    {
        var normalized = name.ToLower();
        return await _shiftTemplateRepository.Query()
            .AsNoTracking()
            .Where(t => t.ShiftTemplateSites.Any(l => l.SiteId == siteId))
            .AnyAsync(t => t.Name.ToLower() == normalized, cancellationToken);
    }

    private async Task<List<ShiftEntity>> LoadDayShiftsAsync(
        int siteId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        return await _shiftRepository.Query(false)
            .Where(s => s.SiteId == siteId && s.StartTime < dayEnd && s.EndTime > dayStart)
            .OrderBy(s => s.StartTime)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);
    }

    private static (TimeSpan Start, TimeSpan End) ComputeTemplateRange(List<ShiftEntity> shifts)
    {
        var templateStart = shifts.Min(s => s.StartTime.TimeOfDay);
        var templateEnd = shifts.Max(s => s.EndTime.TimeOfDay);
        return (templateStart, templateEnd);
    }

    private async Task<Result<ShiftTemplate>> PersistAsync(
        SaveAsTemplateCommand request,
        string name,
        List<ShiftEntity> shifts,
        TimeSpan templateStart,
        TimeSpan templateEnd,
        CancellationToken cancellationToken)
    {
        // Serializable closes the check-then-create race: a concurrent request
        // inserting the same site-scoped name blocks until this transaction
        // commits, then observes the row (or deadlocks and is translated above).
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (await IsDuplicateNameAsync(request.SiteId, name, cancellationToken))
            {
                return Result<ShiftTemplate>.ValidationFailure(DuplicateNameMessage);
            }

            var template = new ShiftTemplate
            {
                Name = name,
                StartTime = templateStart,
                EndTime = templateEnd,
                UpdatedAt = DateTimeOffset.UtcNow,
                LastUpdatedByEmployeeId = request.ActorEmployeeId
            };

            await _shiftTemplateRepository.AddAsync(template, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            template.ShiftTemplateSites.Add(new ShiftTemplateSite
            {
                ShiftTemplateId = template.Id,
                SiteId = request.SiteId
            });

            foreach (var shift in shifts)
            {
                template.ShiftBlocks.Add(ToTemplateBlock(template.Id, shift));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = await _shiftTemplateRepository.Query()
                .AsNoTracking()
                .Include(t => t.ShiftBlocks)
                .FirstAsync(t => t.Id == template.Id, cancellationToken);

            return Result<ShiftTemplate>.Success(created);
        }, IsolationLevel.Serializable, cancellationToken);
    }

    private static ShiftBlock ToTemplateBlock(int templateId, ShiftEntity shift)
    {
        // Snapshot the canvas as-is: overlapping blocks stay independent and
        // the employee assignment is copied exactly (including null and reuse
        // of the same employee across blocks). No dispatch-policy field exists
        // on ShiftBlock: dispatch is request-time only (IsPublished on ShiftEntity).
        return new ShiftBlock
        {
            ShiftTemplateId = templateId,
            StartTime = shift.StartTime.TimeOfDay,
            EndTime = shift.EndTime.TimeOfDay,
            JobRoleId = shift.JobRoleId,
            EmployeeId = shift.EmployeeId
        };
    }
}
