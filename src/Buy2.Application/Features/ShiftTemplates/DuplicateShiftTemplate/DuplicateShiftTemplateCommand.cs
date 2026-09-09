using System.Diagnostics;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.ShiftTemplates.DuplicateShiftTemplate;

public record DuplicateShiftTemplateCommand(int Id, int? ActorEmployeeId)
    : IRequest<Result<DuplicateShiftTemplateResponseDto>>;

public class DuplicateShiftTemplateCommandHandler
    : IRequestHandler<DuplicateShiftTemplateCommand, Result<DuplicateShiftTemplateResponseDto>>
{
    public const int MaxTemplateNameLength = 100;

    private readonly IRepository<ShiftTemplate> _shiftTemplateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DuplicateShiftTemplateCommandHandler(
        IRepository<ShiftTemplate> shiftTemplateRepository,
        IUnitOfWork unitOfWork)
    {
        _shiftTemplateRepository = shiftTemplateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DuplicateShiftTemplateResponseDto>> Handle(
        DuplicateShiftTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var source = await _shiftTemplateRepository.Query(asNoTracking: false)
            .Include(t => t.ShiftTemplateSites)
            .Include(t => t.ShiftBlocks)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (source is null)
        {
            return Result<DuplicateShiftTemplateResponseDto>.NotFound(
                $"Shift template with ID {request.Id} was not found.");
        }

        var newName = await GenerateCopyNameAsync(source.Name, cancellationToken);
        var now = DateTime.UtcNow;

        var duplicate = new ShiftTemplate
        {
            OrganizationId = source.OrganizationId,
            Name = newName,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            CreatedAt = now,
            UpdatedAt = new DateTimeOffset(now, TimeSpan.Zero),
            LastUpdatedByEmployeeId = request.ActorEmployeeId
        };

        foreach (var siteId in source.ShiftTemplateSites.Select(s => s.SiteId).Distinct().OrderBy(id => id))
        {
            duplicate.ShiftTemplateSites.Add(new ShiftTemplateSite { SiteId = siteId });
        }

        foreach (var block in source.ShiftBlocks.OrderBy(b => b.Id))
        {
            duplicate.ShiftBlocks.Add(new ShiftBlock
            {
                StartTime = block.StartTime,
                EndTime = block.EndTime,
                JobRoleId = block.JobRoleId,
                EmployeeId = null
            });
        }

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _shiftTemplateRepository.AddAsync(duplicate, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }, cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<DuplicateShiftTemplateResponseDto>.Conflict(
                "Could not duplicate the shift template. Please try again.");
        }

        return Result<DuplicateShiftTemplateResponseDto>.Success(
            new DuplicateShiftTemplateResponseDto(duplicate.Id, duplicate.Name));
    }

    private async Task<string> GenerateCopyNameAsync(string sourceName, CancellationToken cancellationToken)
    {
        var index = 1;
        while (await NameExistsAsync(BuildCandidateName(sourceName, index), cancellationToken))
        {
            index++;
        }

        return BuildCandidateName(sourceName, index);
    }

    private async Task<bool> NameExistsAsync(string candidate, CancellationToken cancellationToken)
    {
        var lowered = candidate.ToLower();
        return await _shiftTemplateRepository.Query()
            .AnyAsync(t => t.Name.ToLower() == lowered, cancellationToken);
    }

    public static string BuildCandidateName(string sourceName, int index)
    {
        var suffix = $"_copy{index}";
        var keep = MaxTemplateNameLength - suffix.Length;
        var head = sourceName.Length <= keep ? sourceName : sourceName.Substring(0, keep);
        var candidate = head + suffix;

        Debug.Assert(candidate.Length <= MaxTemplateNameLength);
        return candidate;
    }
}
