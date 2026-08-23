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

public record PayrollRecordDto(
    int Id,
    int EmployeeId,
    decimal BaseSalary,
    decimal OvertimePay,
    decimal BonusPay,
    decimal Deductions,
    decimal NetPay,
    DateTime PeriodStartDate,
    DateTime PeriodEndDate,
    string PaymentStatus,
    DateTime? PaidAt
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
    List<string> OfflineWorkdays,
    List<PayrollRecordDto> PayrollRecords
);

