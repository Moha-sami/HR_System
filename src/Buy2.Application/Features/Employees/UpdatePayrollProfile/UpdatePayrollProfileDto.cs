using Buy2.Domain.Enums;

namespace Buy2.Application.Features.Employees.UpdatePayrollProfile;

public record UpdatePayrollProfileDto(
    SalaryType? SalaryType = null,
    string? PayoutPeriod = null,
    int? PayoutDay = null,
    DayOfWeek? WorkWeekStart = null,
    DayOfWeek? WorkWeekEnd = null,
    decimal? PaymentAmount = null,
    decimal? OvertimeThresholdHours = null,
    decimal? OvertimeHourlyRate = null,
    string? AttendanceType = null,
    List<int>? WorkSiteIds = null,
    List<string>? OnlineWorkdays = null,
    List<string>? OfflineWorkdays = null
);
