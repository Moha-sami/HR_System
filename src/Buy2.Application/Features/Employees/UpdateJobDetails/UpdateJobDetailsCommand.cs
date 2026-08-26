using System.Text.Json;
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
    private readonly IUnitOfWork _unitOfWork;

    public UpdateJobDetailsCommandHandler(
        IRepository<Employee> employeeRepository,
        IRepository<JobRole> jobRoleRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _jobRoleRepository = jobRoleRepository;
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
        JobRole? jobRoleToUpdate = null;

        if (dto.JobRoleId.HasValue)
        {
            jobRoleToUpdate = await _jobRoleRepository.GetByIdAsync(dto.JobRoleId.Value);
            if (jobRoleToUpdate is null)
            {
                return UpdateJobDetailsResult.BadRequest($"JobRole with ID {dto.JobRoleId.Value} does not exist.");
            }
            employee.JobRoleId = dto.JobRoleId.Value;
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

        if (dto.OnlineWorkdays is not null)
        {
            employee.OnlineWorkdaysJson = JsonSerializer.Serialize(dto.OnlineWorkdays);
        }

        if (dto.OfflineWorkdays is not null)
        {
            employee.OfflineWorkdaysJson = JsonSerializer.Serialize(dto.OfflineWorkdays);
        }

        if (dto.Qualifications is not null)
        {
            if (jobRoleToUpdate is null && employee.JobRoleId > 0)
            {
                jobRoleToUpdate = await _jobRoleRepository.GetByIdAsync(employee.JobRoleId);
            }

            if (jobRoleToUpdate is not null)
            {
                jobRoleToUpdate.RequiredQualificationsJson = JsonSerializer.Serialize(dto.Qualifications);
                _jobRoleRepository.Update(jobRoleToUpdate);
            }
        }

        _employeeRepository.Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UpdateJobDetailsResult.Success();
    }
}
