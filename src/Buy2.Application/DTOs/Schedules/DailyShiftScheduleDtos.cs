namespace Buy2.Application.DTOs.Schedules;

public enum WeekDayCalendarStatus
{
    CoveredAndPublished,
    MissingResourcesOrUnpublished,
    OvertimeOrMisallocation,
    NoAllocations,
    DimmedDayOff
}

public record CalendarDayBadgeDto(
    DateOnly Date,
    DayOfWeek DayOfWeek,
    WeekDayCalendarStatus Status,
    bool IsSelected,
    bool IsDayOff
);

public record DailyShiftBlockDto(
    int ShiftId,
    int SiteId,
    int JobRoleId,
    string RoleTitle,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsPublished,
    int? EmployeeId,
    string? EmployeeName,
    string? EmployeeAvatarUrl,
    string StatusColorCode
);

public record TimelineHourlyIntervalDto(
    TimeOnly StartHour,
    TimeOnly EndHour,
    List<DailyShiftBlockDto> Blocks
);

public record DailyShiftScheduleResponseDto(
    int SiteId,
    string SiteName,
    DateOnly Date,
    bool IsDayOff,
    decimal TotalEstimatedLaborCost,
    List<CalendarDayBadgeDto> WeekCalendarStrip,
    List<TimelineHourlyIntervalDto> HourlyTimeline
);
