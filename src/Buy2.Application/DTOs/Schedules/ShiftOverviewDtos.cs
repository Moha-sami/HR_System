namespace Buy2.Application.DTOs.Schedules;

public enum ShiftCoverageHealthStatus
{
    Covered,
    Shortage,
    OvertimeRisk,
    UnqualifiedAssignment
}

public record SiteShiftOverviewCardDto(
    int SiteId,
    string SiteName,
    string Address,
    int RegionId,
    string RegionName,
    int TotalShifts,
    int OpenShifts,
    int FilledShifts,
    ShiftCoverageHealthStatus Status,
    bool IsSmartAssignmentEnabled,
    bool IsSmartPostingEnabled
);

public record SiteShiftsOverviewPaginatedResponseDto(
    List<SiteShiftOverviewCardDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
