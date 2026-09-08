using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Application.Features.ShiftTemplates.Validators;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.ShiftTemplates.UpdateShiftTemplate;

public record UpdateShiftTemplateCommand(int Id, UpdateShiftTemplateDto Dto, int? ActorEmployeeId)
    : IRequest<Result<ShiftTemplateDetailsDto>>;

public class UpdateShiftTemplateCommandHandler
    : IRequestHandler<UpdateShiftTemplateCommand, Result<ShiftTemplateDetailsDto>>
{
    private readonly IRepository<ShiftTemplate> _shiftTemplateRepository;
    private readonly IRepository<ShiftTemplateSite> _shiftTemplateSiteRepository;
    private readonly IRepository<ShiftBlock> _shiftBlockRepository;
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<JobRole> _jobRoleRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateShiftTemplateCommandHandler(
        IRepository<ShiftTemplate> shiftTemplateRepository,
        IRepository<ShiftTemplateSite> shiftTemplateSiteRepository,
        IRepository<ShiftBlock> shiftBlockRepository,
        IRepository<Site> siteRepository,
        IRepository<JobRole> jobRoleRepository,
        IRepository<Employee> employeeRepository,
        IUnitOfWork unitOfWork)
    {
        _shiftTemplateRepository = shiftTemplateRepository;
        _shiftTemplateSiteRepository = shiftTemplateSiteRepository;
        _shiftBlockRepository = shiftBlockRepository;
        _siteRepository = siteRepository;
        _jobRoleRepository = jobRoleRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ShiftTemplateDetailsDto>> Handle(
        UpdateShiftTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _shiftTemplateRepository.Query(asNoTracking: false)
            .Include(t => t.ShiftBlocks)
            .Include(t => t.ShiftTemplateSites)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (template is null)
        {
            return Result<ShiftTemplateDetailsDto>.NotFound(
                $"Shift template with ID {request.Id} was not found.");
        }

        var dto = request.Dto;

        var validation = await new UpdateShiftTemplateDtoValidator().ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<ShiftTemplateDetailsDto>.ValidationFailure(
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        if (!ShiftTimeHelper.TryParseTime(dto.StartTime, out var templateStart)
            || !ShiftTimeHelper.TryParseTime(dto.EndTime, out var templateEnd))
        {
            return Result<ShiftTemplateDetailsDto>.ValidationFailure(
                "StartTime and EndTime must be valid times in 'hh:mm tt' format (e.g. '09:00 AM').");
        }

        var distinctSiteIds = dto.SiteIds.Distinct().ToList();
        var blocks = dto.ShiftBlocks
            .GroupBy(b => b.Id)
            .Select(g => g.First())
            .ToList();
        var employeeIds = blocks.Select(b => b.AssignedUserId).Distinct().ToList();
        var roleIds = blocks.Select(b => b.JobRoleId).Distinct().ToList();

        var sitesCheck = await ShiftTemplateExistenceValidator.EnsureSitesExistAsync(
            _siteRepository, distinctSiteIds, cancellationToken);
        if (!sitesCheck.IsSuccess)
        {
            return Result<ShiftTemplateDetailsDto>.ValidationFailure(
                sitesCheck.ErrorMessage ?? "One or more SiteIds do not exist.");
        }

        var rolesCheck = await ShiftTemplateExistenceValidator.EnsureRolesExistAsync(
            _jobRoleRepository, roleIds, cancellationToken);
        if (!rolesCheck.IsSuccess)
        {
            return Result<ShiftTemplateDetailsDto>.ValidationFailure(
                rolesCheck.ErrorMessage ?? "One or more JobRoleIds do not exist.");
        }

        var employeesCheck = await ShiftTemplateExistenceValidator.EnsureEmployeesExistAsync(
            _employeeRepository, employeeIds, cancellationToken);
        if (!employeesCheck.IsSuccess)
        {
            return Result<ShiftTemplateDetailsDto>.ValidationFailure(
                employeesCheck.ErrorMessage ?? "One or more AssignedUserIds do not exist.");
        }

        var duplicateCheck = ShiftTemplateAssignmentValidator.EnsureNoDuplicateEmployees(
            blocks.Select(b => b.AssignedUserId).ToList());
        if (!duplicateCheck.IsSuccess)
        {
            return Result<ShiftTemplateDetailsDto>.Conflict(
                duplicateCheck.ErrorMessage ?? "An employee cannot be assigned to more than one block in the same shift.");
        }

        var collisionCheck = await ShiftTemplateAssignmentValidator.EnsureEmployeesNotAssignedElsewhereAsync(
            _shiftBlockRepository, employeeIds, excludeTemplateId: request.Id, cancellationToken);
        if (!collisionCheck.IsSuccess)
        {
            return Result<ShiftTemplateDetailsDto>.Conflict(
                collisionCheck.ErrorMessage ?? "One or more employees are already assigned to another shift.");
        }

        var existingById = template.ShiftBlocks.ToDictionary(b => b.Id);
        var ownershipCheck = ShiftTemplateExistenceValidator.EnsureBlocksBelongToTemplate(
            existingById, blocks.Select(b => b.Id).ToList());
        if (!ownershipCheck.IsSuccess)
        {
            return Result<ShiftTemplateDetailsDto>.NotFound(
                ownershipCheck.ErrorMessage ?? "One or more shift blocks were not found in this shift template.");
        }

        try
        {
            var details = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                template.Name = dto.Name.Trim();
                template.StartTime = templateStart;
                template.EndTime = templateEnd;
                template.UpdatedAt = DateTimeOffset.UtcNow;
                template.LastUpdatedByEmployeeId = request.ActorEmployeeId;
                _shiftTemplateRepository.Update(template);

                var desiredSiteIds = distinctSiteIds.ToHashSet();
                var existingSiteIds = template.ShiftTemplateSites.Select(s => s.SiteId).ToHashSet();

                var staleLinks = template.ShiftTemplateSites
                    .Where(s => !desiredSiteIds.Contains(s.SiteId))
                    .ToList();
                foreach (var stale in staleLinks)
                {
                    _shiftTemplateSiteRepository.Delete(stale);
                }

                foreach (var siteId in distinctSiteIds.Where(id => !existingSiteIds.Contains(id)))
                {
                    await _shiftTemplateSiteRepository.AddAsync(new ShiftTemplateSite
                    {
                        ShiftTemplateId = template.Id,
                        SiteId = siteId
                    });
                }

                var incomingById = blocks.ToDictionary(b => b.Id);

                var removedBlocks = template.ShiftBlocks
                    .Where(b => !incomingById.ContainsKey(b.Id))
                    .ToList();
                foreach (var removed in removedBlocks)
                {
                    _shiftBlockRepository.Delete(removed);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                foreach (var retained in template.ShiftBlocks.Where(b => incomingById.ContainsKey(b.Id)))
                {
                    var input = incomingById[retained.Id];
                    ShiftTimeHelper.TryParseTime(input.StartTime, out var blockStart);
                    ShiftTimeHelper.TryParseTime(input.EndTime, out var blockEnd);

                    retained.StartTime = blockStart;
                    retained.EndTime = blockEnd;
                    retained.JobRoleId = input.JobRoleId;
                    retained.EmployeeId = input.AssignedUserId;
                    _shiftBlockRepository.Update(retained);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var updated = await _shiftTemplateRepository.Query()
                    .AsNoTracking()
                    .Include(t => t.ShiftTemplateSites)
                        .ThenInclude(s => s.Site)
                    .Include(t => t.ShiftBlocks)
                        .ThenInclude(b => b.JobRole)
                    .Include(t => t.ShiftBlocks)
                        .ThenInclude(b => b.Employee)
                    .FirstAsync(t => t.Id == template.Id, cancellationToken);

                return ShiftTemplateMapper.ToDetailsDto(updated);
            }, cancellationToken);

            return Result<ShiftTemplateDetailsDto>.Success(details);
        }
        catch (DbUpdateException)
        {
            return Result<ShiftTemplateDetailsDto>.Conflict(
                "One or more employees are already assigned to another shift.");
        }
    }
}
