using Buy2.Domain.Enums;

namespace Buy2.Application.Features.Employees.UpdatePayrollProfile;

public record UpdatePayrollProfileDto(
    SalaryType? SalaryType = null,
    string? PayoutPeriod = null,
    int? PayoutDay = null,
    DayOfWeek? WorkWeekStartDay = null,
    DayOfWeek? WorkWeekEndDay = null,
    decimal? PaymentAmount = null,
    decimal? OvertimeThresholdHours = null,
    decimal? OvertimeRateMultiplier = null,
    string? AttendanceType = null,
    List<int>? AssignedWorkSiteIds = null,
    List<string>? OnlineWorkdays = null,
    List<string>? OfflineWorkdays = null
);
