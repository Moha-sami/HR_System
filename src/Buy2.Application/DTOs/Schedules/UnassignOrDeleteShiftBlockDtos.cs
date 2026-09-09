namespace Buy2.Application.DTOs.Schedules;

public enum ShiftBlockRemovalAction
{
    UnassignOnly = 0,
    DeleteBlock = 1
}

public record UnassignOrDeleteShiftBlockRequestDto(
    ShiftBlockRemovalAction Action = ShiftBlockRemovalAction.UnassignOnly,
    bool ConfirmPublishedDeletion = false
);

public record UnassignOrDeleteShiftBlockResponseDto(
    int ShiftBlockId,
    ShiftBlockRemovalAction ActionTaken,
    bool IsDeleted,
    DailyShiftBlockDto? UpdatedBlock,
    decimal UpdatedDailyLaborCost,
    WeekDayCalendarStatus SiteCoverageStatus
);
