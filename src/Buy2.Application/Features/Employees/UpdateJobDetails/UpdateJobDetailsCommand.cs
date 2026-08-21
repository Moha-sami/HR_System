using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Employees.UpdateJobDetails;

public record UpdateJobDetailsResult(bool IsSuccess, bool IsNotFound, string? ErrorMessage = null)
{
    public static UpdateJobDetailsResult Success() => new(true, false);
    public static UpdateJobDetailsResult NotFound(string message = "Employee not found.") => new(false, true, message);
    public static UpdateJobDetailsResult BadRequest(string message) => new(false, false, message);
}

public record UpdateJobDetailsCommand(int EmployeeId, UpdateJobDetailsDto Dto) : IRequest<UpdateJobDetailsResult>;

public class UpdateJobDetailsCommandHandler : IRequestHandler<UpdateJobDetailsCommand, UpdateJobDetailsResult>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<JobRole> _jobRoleRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Site> _siteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateJobDetailsCommandHandler(
        IRepository<Employee> employeeRepository,
        IRepository<JobRole> jobRoleRepository,
        IRepository<Role> roleRepository,
        IRepository<Site> siteRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _jobRoleRepository = jobRoleRepository;
        _roleRepository = roleRepository;
        _siteRepository = siteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateJobDetailsResult> Handle(UpdateJobDetailsCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId);

        if (employee is null || employee.IsDeleted)
        {
            return UpdateJobDetailsResult.NotFound($"Employee with ID {request.EmployeeId} was not found.");
        }

        var dto = request.Dto;

        if (dto.JobRoleId.HasValue)
        {
            var jobRole = await _jobRoleRepository.GetByIdAsync(dto.JobRoleId.Value);
            if (jobRole is null)
            {
                return UpdateJobDetailsResult.BadRequest($"JobRole with ID {dto.JobRoleId.Value} does not exist.");
            }
            employee.JobRoleId = dto.JobRoleId.Value;
        }

        if (dto.RoleId.HasValue)
        {
            var role = await _roleRepository.GetByIdAsync(dto.RoleId.Value);
            if (role is null)
            {
                return UpdateJobDetailsResult.BadRequest($"Role with ID {dto.RoleId.Value} does not exist.");
            }
            employee.RoleId = dto.RoleId.Value;
        }

        if (dto.SiteId.HasValue)
        {
            var site = await _siteRepository.GetByIdAsync(dto.SiteId.Value);
            if (site is null)
            {
                return UpdateJobDetailsResult.BadRequest($"Site with ID {dto.SiteId.Value} does not exist.");
            }
            employee.SiteId = dto.SiteId.Value;
        }

        if (dto.DirectManagerId.HasValue)
        {
            if (dto.DirectManagerId.Value == request.EmployeeId)
            {
                return UpdateJobDetailsResult.BadRequest("An employee cannot be their own direct manager.");
            }

            var manager = await _employeeRepository.GetByIdAsync(dto.DirectManagerId.Value);
            if (manager is null || manager.IsDeleted)
            {
                return UpdateJobDetailsResult.BadRequest($"Direct manager with ID {dto.DirectManagerId.Value} does not exist or has been deleted.");
            }

            employee.DirectManagerId = dto.DirectManagerId.Value;
        }

        if (dto.SeniorityLevel is not null)
        {
            employee.SeniorityLevel = dto.SeniorityLevel;
        }

        if (dto.ExperienceYears.HasValue)
        {
            employee.ExperienceYears = dto.ExperienceYears.Value;
        }

        if (dto.JobType is not null)
        {
            employee.JobType = dto.JobType;
        }

        if (dto.AttendanceType is not null)
        {
            employee.AttendanceType = dto.AttendanceType;
        }

        if (dto.JoinDate.HasValue)
        {
            employee.JoinDate = dto.JoinDate.Value.UtcDateTime;
        }

        _employeeRepository.Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UpdateJobDetailsResult.Success();
    }
}
