using System.Text.Json;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Employees;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.GetEmployee;

public record GetEmployeeProfileQuery(int Id) : IRequest<EmployeeProfileDto?>;

public class GetEmployeeProfileQueryHandler : IRequestHandler<GetEmployeeProfileQuery, EmployeeProfileDto?>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<PointsTransaction> _pointsRepository;
    private readonly IRepository<EmployeeTask> _tasksRepository;
    private readonly IRepository<RewardRedemption> _redemptionRepository;

    public GetEmployeeProfileQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<PointsTransaction> pointsRepository,
        IRepository<EmployeeTask> tasksRepository,
        IRepository<RewardRedemption> redemptionRepository)
    {
        _employeeRepository = employeeRepository;
        _pointsRepository = pointsRepository;
        _tasksRepository = tasksRepository;
        _redemptionRepository = redemptionRepository;
    }

    public async Task<EmployeeProfileDto?> Handle(GetEmployeeProfileQuery request, CancellationToken cancellationToken)
    {
        // 1. Fetch Employee with eager loaded navigations
        var employee = await _employeeRepository.Query()
            .Include(e => e.JobRole)
            .Include(e => e.Site)
            .Include(e => e.DirectManager)
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (employee == null)
        {
            return null;
        }

        // 2. Calculate live stats
        var totalPoints = await _pointsRepository.Query()
            .Where(p => p.EmployeeId == request.Id)
            .SumAsync(p => (int?)p.Amount, cancellationToken) ?? 0;

        var totalTasks = await _tasksRepository.Query()
            .Where(t => t.EmployeeId == request.Id)
            .CountAsync(cancellationToken);

        var totalGifts = await _redemptionRepository.Query()
            .Where(r => r.EmployeeId == request.Id)
            .CountAsync(cancellationToken);

        var stats = new EmployeeStatsDto(
            TotalPoints: totalPoints,
            TotalTasks: totalTasks,
            TotalGifts: totalGifts
        );

        // 3. Extract Qualifications
        var qualifications = new List<string>();
        if (!string.IsNullOrWhiteSpace(employee.JobRole?.RequiredQualificationsJson))
        {
            try
            {
                qualifications = JsonSerializer.Deserialize<List<string>>(employee.JobRole.RequiredQualificationsJson)
                    ?? new List<string>();
            }
            catch
            {
                qualifications = new List<string>();
            }
        }

        // 4. Map Personal Info
        var personalInfo = new EmployeePersonalInfoDto(
            Birthdate: employee.Birthdate,
            Gender: employee.Gender
        );

        // 5. Map Job Details
        var managerName = employee.DirectManager != null
            ? $"{employee.DirectManager.FirstName} {employee.DirectManager.LastName}".Trim()
            : null;

        var department = employee.JobRole != null
            ? (employee.JobRole.DepartmentId > 0 ? $"Department #{employee.JobRole.DepartmentId}" : "N/A")
            : "N/A";

        var jobDetails = new EmployeeJobDetailsDto(
            Title: employee.JobRole?.Title ?? "N/A",
            Department: department,
            SeniorityLevel: employee.SeniorityLevel ?? string.Empty,
            ExperienceYears: employee.ExperienceYears,
            DirectManagerName: managerName,
            JobType: employee.JobType ?? string.Empty,
            Qualifications: qualifications
        );

        // 6. Assemble Full Profile DTO
        var fullName = $"{employee.FirstName} {employee.LastName}".Trim();
        var employeeCode = string.IsNullOrEmpty(employee.EmployeeCode) ? $"EMP-{employee.Id:D4}" : employee.EmployeeCode;
        var location = employee.Site?.SiteName ?? "N/A";

        return new EmployeeProfileDto(
            Id: employee.Id,
            EmployeeCode: employeeCode,
            FullName: fullName,
            Phone: employee.PhoneNumber ?? string.Empty,
            Email: employee.Email ?? string.Empty,
            Location: location,
            ProfilePhotoUrl: employee.ProfilePhotoUrl,
            Stats: stats,
            PersonalInfo: personalInfo,
            JobDetails: jobDetails
        );
    }
}
