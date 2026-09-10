namespace Buy2.Application.DTOs.Schedules;

public enum PublishExceptionDecision
{
    Publish = 1,
    Skip = 2
}

public record CommitPublishScheduleRequestDto(
    int SiteId,
    List<DateOnly>? TargetDates = null,
    bool AllUnpublishedDays = false,
    List<int>? TargetRoleIds = null,
    Dictionary<int, PublishExceptionDecision>? ExceptionResolutions = null,
    string? OvertimeJustification = null
);

public record CommitPublishScheduleResponseDto(
    bool Success,
    int SiteId,
    int TotalShiftsProcessed,
    int PublishedImmediatelyCount,
    int PendingHrApprovalCount,
    int SkippedCount,
    List<int> PublishedShiftIds,
    List<int> PendingHrApprovalShiftIds,
    List<int> SkippedShiftIds,
    string Message
);
