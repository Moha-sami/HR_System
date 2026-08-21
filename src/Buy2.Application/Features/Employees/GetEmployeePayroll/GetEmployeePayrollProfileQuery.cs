using System.Text.Json;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Employees;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.GetEmployeePayroll;

public record GetEmployeePayrollProfileQuery(int EmployeeId) : IRequest<EmployeePayrollProfileDto?>;

public class GetEmployeePayrollProfileQueryHandler : IRequestHandler<GetEmployeePayrollProfileQuery, EmployeePayrollProfileDto?>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<PayrollProfile> _payrollProfileRepository;

    public GetEmployeePayrollProfileQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<PayrollProfile> payrollProfileRepository)
    {
        _employeeRepository = employeeRepository;
        _payrollProfileRepository = payrollProfileRepository;
    }

    public async Task<EmployeePayrollProfileDto?> Handle(GetEmployeePayrollProfileQuery request, CancellationToken cancellationToken)
    {
        // 1. Fetch Employee with navigation properties
        var employee = await _employeeRepository.Query()
            .Include(e => e.EmployeeSites)
            .Include(e => e.PayrollProfile)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null || employee.IsDeleted)
        {
            return null;
        }

        // 2. Fetch PayrollProfile (from navigation or fallback query)
        var payroll = employee.PayrollProfile;
        if (payroll == null)
        {
            payroll = await _payrollProfileRepository.Query()
                .FirstOrDefaultAsync(p => p.EmployeeId == request.EmployeeId, cancellationToken);
        }

        var isConfigured = payroll != null;

        // 3. Helper functions for safe parsing
        static List<T> ParseJsonList<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<T>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
        }

        static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue) where TEnum : struct, Enum
        {
            if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<TEnum>(value, true, out var result))
            {
                return result;
            }

            return defaultValue;
        }

        // 4. Resolve WorkSiteIds
        var workSiteIds = new List<int>();
        if (employee.EmployeeSites != null)
        {
            workSiteIds = employee.EmployeeSites
                .Select(es => es.SiteId)
                .Distinct()
                .ToList();
        }

        if (workSiteIds.Count == 0 && !string.IsNullOrWhiteSpace(payroll?.WorkSiteIdsJson))
        {
            workSiteIds = ParseJsonList<int>(payroll.WorkSiteIdsJson);
        }

        if (workSiteIds.Count == 0 && employee.SiteId > 0)
        {
            workSiteIds.Add(employee.SiteId);
        }

        // 5. Safe workdays JSON deserialization
        var onlineWorkdays = ParseJsonList<string>(employee.OnlineWorkdaysJson);
        if (onlineWorkdays.Count == 0 && payroll != null)
        {
            onlineWorkdays = ParseJsonList<string>(payroll.OnlineWorkdaysJson);
        }

        var offlineWorkdays = ParseJsonList<string>(employee.OfflineWorkdaysJson);
        if (offlineWorkdays.Count == 0 && payroll != null)
        {
            offlineWorkdays = ParseJsonList<string>(payroll.OfflineWorkdaysJson);
        }

        // 6. Resolve AttendanceType
        var attendanceType = employee.AttendanceType;
        if (string.IsNullOrWhiteSpace(attendanceType))
        {
            attendanceType = payroll?.AttendanceType ?? string.Empty;
        }

        // 7. Resolve SalaryType & Payout details
        var salaryType = ParseEnum(payroll?.SalaryType, SalaryType.Fixed);
        var payoutPeriod = payroll?.PayoutPeriod ?? string.Empty;
        var payoutDay = payroll?.PayoutDay ?? 0;
        var workWeekStart = ParseEnum(payroll?.WorkWeekStart, DayOfWeek.Sunday);
        var workWeekEnd = ParseEnum(payroll?.WorkWeekEnd, DayOfWeek.Thursday);

        var paymentAmount = payroll?.PaymentAmount ?? 0m;
        var overtimeThresholdHours = payroll?.OvertimeThresholdHours ?? 0m;
        var overtimeHourlyRate = payroll?.OvertimeHourlyRate ?? 0m;

        // 7. Assemble and return DTO
        return new EmployeePayrollProfileDto(
            EmployeeId: employee.Id,
            IsConfigured: isConfigured,
            SalaryType: salaryType,
            PayoutPeriod: payoutPeriod,
            PayoutDay: payoutDay,
            WorkWeekStart: workWeekStart,
            WorkWeekEnd: workWeekEnd,
            PaymentAmount: paymentAmount,
            OvertimeThresholdHours: overtimeThresholdHours,
            OvertimeHourlyRate: overtimeHourlyRate,
            AttendanceType: attendanceType,
            WorkSiteIds: workSiteIds,
            OnlineWorkdays: onlineWorkdays,
            OfflineWorkdays: offlineWorkdays
        );
    }
}
