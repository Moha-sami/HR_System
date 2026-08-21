using Buy2.Domain.Enums;

namespace Buy2.Application.DTOs.Employees;

public record PayrollProfileDto(
    SalaryType SalaryType,
    string PayoutPeriod,
    int PayoutDay,
    DayOfWeek WorkWeekStart,
    DayOfWeek WorkWeekEnd,
    decimal PaymentAmount,
    decimal OvertimeThresholdHours,
    decimal OvertimeHourlyRate,
    string AttendanceType,
    List<int> WorkSiteIds,
    List<string> OnlineWorkdays,
    List<string> OfflineWorkdays
);

public record EmployeePayrollProfileDto(
    int EmployeeId,
    bool IsConfigured,
    SalaryType SalaryType,
    string PayoutPeriod,
    int PayoutDay,
    DayOfWeek WorkWeekStart,
    DayOfWeek WorkWeekEnd,
    decimal PaymentAmount,
    decimal OvertimeThresholdHours,
    decimal OvertimeHourlyRate,
    string AttendanceType,
    List<int> WorkSiteIds,
    List<string> OnlineWorkdays,
    List<string> OfflineWorkdays
);

