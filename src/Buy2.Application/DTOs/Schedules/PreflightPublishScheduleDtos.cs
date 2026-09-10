namespace Buy2.Application.DTOs.Schedules;

public record PreflightPublishScheduleRequestDto(
    int SiteId,
    List<DateOnly>? TargetDates = null,
    bool AllUnpublishedDays = false,
    List<int>? TargetRoleIds = null
);

public record UnqualifiedAssigneeExceptionDto(
    int ShiftId,
    int EmployeeId,
    string EmployeeName,
    int RequiredRoleId,
    string RequiredRoleTitle,
    int EmployeeRoleId,
    string EmployeeRoleTitle,
    DateOnly Date,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string FormattedTime
);

public record OvertimeViolationExceptionDto(
    int ShiftId,
    int EmployeeId,
    string EmployeeName,
    int JobRoleId,
    string JobRoleTitle,
    DateOnly Date,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string FormattedTime,
    decimal ShiftHours,
    decimal TotalWeeklyHours,
    decimal ProjectedOtHours,
    string ViolationReason
);

public record PreflightPublishScheduleResponseDto(
    int SiteId,
    int TotalUnpublishedShiftsScanned,
    int TotalDatesScanned,
    List<DateOnly> ScannedDates,
    List<UnqualifiedAssigneeExceptionDto> UnqualifiedAssignees,
    List<OvertimeViolationExceptionDto> OvertimeViolations,
    int UnqualifiedCount,
    int OvertimeCount,
    bool HasExceptions,
    bool CanPublishImmediately
);
