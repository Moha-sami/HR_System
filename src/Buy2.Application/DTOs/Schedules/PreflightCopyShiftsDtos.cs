namespace Buy2.Application.DTOs.Schedules;

public record PreflightCopyShiftsRequestDto(
    int SiteId,
    DateOnly SourceDate,
    List<DateOnly>? TargetDates = null,
    List<DayOfWeek>? RecurringDays = null,
    int? WeekCount = null
);

public record ShiftConflictSummaryDto(
    int ShiftId,
    string ShiftName,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string? JobRoleTitle,
    string? EmployeeName
);

public record ConflictingDateDto(
    DateOnly Date,
    int ShiftCount,
    List<string> ShiftNames,
    List<ShiftConflictSummaryDto> ExistingShifts
);

public record PreflightCopyShiftsResponseDto(
    int SiteId,
    DateOnly SourceDate,
    int TotalTargetDates,
    List<DateOnly> ConflictFreeDates,
    List<ConflictingDateDto> ConflictingDates,
    bool HasConflicts
);
