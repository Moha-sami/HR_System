using Buy2.Application.DTOs.Schedules;

namespace Buy2.Application.DTOs.Sites;

public record SetSiteDayOffRequestDto(bool IsDayOff);

public record SetSiteDayOffResponseDto(
    int SiteId,
    string SiteName,
    DateOnly Date,
    DayOfWeek DayOfWeek,
    bool IsDayOff,
    WeekDayCalendarStatus CoverageStatus,
    int ExistingShiftsCount,
    string Message
);
