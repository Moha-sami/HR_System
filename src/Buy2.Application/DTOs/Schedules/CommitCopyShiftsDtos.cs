namespace Buy2.Application.DTOs.Schedules;

public enum CopyConflictResolution
{
    Replace = 1,
    KeepExisting = 2
}

public record CommitCopyShiftsRequestDto(
    int SiteId,
    DateOnly SourceDate,
    List<DateOnly>? TargetDates = null,
    Dictionary<DateOnly, CopyConflictResolution>? DateResolutions = null,
    bool BulkReplaceAll = false,
    bool CopyAssignments = true
);

public record CommitCopyShiftsResponseDto(
    bool Success,
    int TotalDatesProcessed,
    int CopiedDatesCount,
    int SkippedDatesCount,
    int TotalShiftsCreated,
    int TotalShiftsReplaced,
    List<DateOnly> CopiedDates,
    List<DateOnly> SkippedDates,
    string Message
);
