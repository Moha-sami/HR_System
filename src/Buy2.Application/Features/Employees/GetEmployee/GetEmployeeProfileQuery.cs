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
        var employee = await _employeeRepository.Query(asNoTracking: true)
            .Include(e => e.JobRole)
                .ThenInclude(jr => jr!.Department)
            .Include(e => e.Site)
                .ThenInclude(s => s!.Region)
            .Include(e => e.DirectManager)
            .Include(e => e.Role)
            .Include(e => e.PayrollProfile)
            .Include(e => e.EmployeeSites)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (employee == null || employee.IsDeleted)
        {
            return null;
        }

        // 2. Calculate live stats
        var totalPoints = await _pointsRepository.Query(asNoTracking: true)
            .Where(p => p.EmployeeId == request.Id)
            .SumAsync(p => (int?)p.Amount, cancellationToken) ?? 0;

        var totalTasks = await _tasksRepository.Query(asNoTracking: true)
            .Where(t => t.EmployeeId == request.Id)
            .CountAsync(cancellationToken);

        var totalGifts = await _redemptionRepository.Query(asNoTracking: true)
            .Where(r => r.EmployeeId == request.Id)
            .CountAsync(cancellationToken);

        var stats = new EmployeeStatsDto(
            TotalPoints: totalPoints,
            TotalTasks: totalTasks,
            TotalGifts: totalGifts
        );

        // 3. Map Personal Info
        var fullName = $"{employee.FirstName} {employee.LastName}".Trim();
        var personalInfo = new EmployeePersonalInfoDto(
            Name: fullName,
            Birthdate: employee.Birthdate,
            Email: employee.Email ?? string.Empty,
            PhoneNumber: employee.PhoneNumber ?? string.Empty,
            Gender: employee.Gender
        );

        // 4. Map Job Details
        var directManagerName = (string?)null;
        if (employee.DirectManager != null)
        {
            var managerFullName = $"{employee.DirectManager.FirstName} {employee.DirectManager.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(managerFullName))
            {
                directManagerName = managerFullName;
            }
        }

        var department = employee.JobRole?.Department?.Name ?? "N/A";

        var qualifications = ParseJsonList(employee.JobRole?.RequiredQualificationsJson);
        var onlineWorkdays = ParseJsonList(employee.OnlineWorkdaysJson);
        if (onlineWorkdays.Count == 0 && employee.PayrollProfile != null)
        {
            onlineWorkdays = ParseJsonList(employee.PayrollProfile.OnlineWorkdaysJson);
        }

        var offlineWorkdays = ParseJsonList(employee.OfflineWorkdaysJson);
        if (offlineWorkdays.Count == 0 && employee.PayrollProfile != null)
        {
            offlineWorkdays = ParseJsonList(employee.PayrollProfile.OfflineWorkdaysJson);
        }

        var attendanceType = employee.AttendanceType;
        if (string.IsNullOrWhiteSpace(attendanceType) && employee.PayrollProfile != null)
        {
            attendanceType = employee.PayrollProfile.AttendanceType;
        }
        if (string.IsNullOrWhiteSpace(attendanceType))
        {
            attendanceType = "OnSite";
        }

        var jobDetails = new EmployeeJobDetailsDto(
            Title: employee.JobRole?.Title ?? "N/A",
            Department: department,
            SeniorityLevel: employee.SeniorityLevel ?? string.Empty,
            ExperienceYears: employee.ExperienceYears,
            DirectManagerName: directManagerName,
            JobType: employee.JobType ?? string.Empty,
            Qualifications: qualifications,
            AttendanceType: attendanceType,
            OnlineWorkdays: onlineWorkdays,
            OfflineWorkdays: offlineWorkdays,
            JobRoleId: employee.JobRoleId > 0 ? employee.JobRoleId : (int?)null,
            DirectManagerId: employee.DirectManagerId
        );

        // 5. Map Payroll Summary
        EmployeePayrollSummaryDto? payrollSummary = null;
        if (employee.PayrollProfile != null)
        {
            var payroll = employee.PayrollProfile;
            var assignedWorkSiteIds = employee.EmployeeSites != null && employee.EmployeeSites.Count > 0
                ? employee.EmployeeSites.Select(es => es.SiteId).Distinct().ToList()
                : new List<int>();

            if (assignedWorkSiteIds.Count == 0 && !string.IsNullOrWhiteSpace(payroll.WorkSiteIdsJson))
            {
                try
                {
                    assignedWorkSiteIds = JsonSerializer.Deserialize<List<int>>(payroll.WorkSiteIdsJson) ?? new List<int>();
                }
                catch
                {
                    assignedWorkSiteIds = new List<int>();
                }
            }

            if (assignedWorkSiteIds.Count == 0 && employee.SiteId > 0)
            {
                assignedWorkSiteIds.Add(employee.SiteId);
            }

            var overtimeEnabled = payroll.OvertimeThresholdHours > 0 || payroll.OvertimeHourlyRate > 0;
            var overtimeThresholdHours = payroll.OvertimeThresholdHours > 0 ? payroll.OvertimeThresholdHours : (decimal?)null;
            var overtimeRateMultiplier = payroll.OvertimeHourlyRate > 0 ? payroll.OvertimeHourlyRate : (decimal?)null;

            payrollSummary = new EmployeePayrollSummaryDto(
                SalaryType: payroll.SalaryType ?? string.Empty,
                PaymentAmount: payroll.PaymentAmount,
                PayoutPeriod: payroll.PayoutPeriod ?? string.Empty,
                PayoutDay: payroll.PayoutDay > 0 ? payroll.PayoutDay : (int?)null,
                WorkWeekStartDay: payroll.WorkWeekStart,
                WorkWeekEndDay: payroll.WorkWeekEnd,
                OvertimeEnabled: overtimeEnabled,
                OvertimeThresholdHours: overtimeThresholdHours,
                OvertimeRateMultiplier: overtimeRateMultiplier,
                AssignedWorkSiteIds: assignedWorkSiteIds
            );
        }

        // 6. Assemble Full Profile DTO
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
            JobDetails: jobDetails,
            Payroll: payrollSummary
        );
    }

    private static List<string> ParseJsonList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
