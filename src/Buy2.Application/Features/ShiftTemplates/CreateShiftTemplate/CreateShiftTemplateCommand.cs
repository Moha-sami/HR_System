using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Application.Features.ShiftTemplates.Validators;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.ShiftTemplates.CreateShiftTemplate;

public record CreateShiftTemplateCommand(CreateShiftTemplateDto Dto, int? ActorEmployeeId)
    : IRequest<Result<ShiftTemplateDetailsDto>>;

public class CreateShiftTemplateCommandHandler
    : IRequestHandler<CreateShiftTemplateCommand, Result<ShiftTemplateDetailsDto>>
{
    private readonly IRepository<ShiftTemplate> _shiftTemplateRepository;
    private readonly IRepository<ShiftTemplateSite> _shiftTemplateSiteRepository;
    private readonly IRepository<ShiftBlock> _shiftBlockRepository;
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<JobRole> _jobRoleRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateShiftTemplateCommandHandler(
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
        CreateShiftTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var validation = await new CreateShiftTemplateDtoValidator().ValidateAsync(dto, cancellationToken);
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
        var blocks = dto.ShiftBlocks.Distinct().ToList();
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
            _shiftBlockRepository, employeeIds, excludeTemplateId: null, cancellationToken);
        if (!collisionCheck.IsSuccess)
        {
            return Result<ShiftTemplateDetailsDto>.Conflict(
                collisionCheck.ErrorMessage ?? "One or more employees are already assigned to another shift.");
        }

        try
        {
            var details = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var template = new ShiftTemplate
                {
                    Name = dto.Name.Trim(),
                    StartTime = templateStart,
                    EndTime = templateEnd,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    LastUpdatedByEmployeeId = request.ActorEmployeeId
                };

                await _shiftTemplateRepository.AddAsync(template);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                foreach (var siteId in distinctSiteIds)
                {
                    await _shiftTemplateSiteRepository.AddAsync(new ShiftTemplateSite
                    {
                        ShiftTemplateId = template.Id,
                        SiteId = siteId
                    });
                }

                foreach (var block in blocks)
                {
                    ShiftTimeHelper.TryParseTime(block.StartTime, out var blockStart);
                    ShiftTimeHelper.TryParseTime(block.EndTime, out var blockEnd);

                    await _shiftBlockRepository.AddAsync(new ShiftBlock
                    {
                        ShiftTemplateId = template.Id,
                        StartTime = blockStart,
                        EndTime = blockEnd,
                        JobRoleId = block.JobRoleId,
                        EmployeeId = block.AssignedUserId
                    });
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var created = await _shiftTemplateRepository.Query()
                    .AsNoTracking()
                    .Include(t => t.ShiftTemplateSites)
                        .ThenInclude(s => s.Site)
                    .Include(t => t.ShiftBlocks)
                        .ThenInclude(b => b.JobRole)
                    .Include(t => t.ShiftBlocks)
                        .ThenInclude(b => b.Employee)
                    .FirstAsync(t => t.Id == template.Id, cancellationToken);

                return ShiftTemplateMapper.ToDetailsDto(created);
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
