namespace Buy2.Application.DTOs.Schedules;

public record AutoFillDailyShiftsRequestDto(int SiteId, DateOnly Date);

public record AutoFillAssignmentDetailDto(
    int ShiftId,
    int EmployeeId,
    string EmployeeName,
    string RoleTitle,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsPreferredEmployee
);

public record AutoFillDailyShiftsResponseDto(
    int SiteId,
    DateOnly Date,
    int TotalOpenBlocksScanned,
    int SuccessfullyAssignedCount,
    int UnfillableCount,
    List<AutoFillAssignmentDetailDto> AssignedBlocks,
    decimal UpdatedDailyLaborCost,
    WeekDayCalendarStatus CoverageStatus,
    string SummaryMessage
);
