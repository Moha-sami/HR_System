namespace Buy2.Application.Features.Employees.GetPerformanceOverview;

public record DateRangeResolvedDto(
    DateTimeOffset From,
    DateTimeOffset To,
    string? Period = null
);

public record TasksSummaryDto(
    int TotalTasks,
    int TodoCount,
    int InProgressCount,
    int CompletedCount,
    int OverdueCount,
    decimal DeadlineCompliancePercentage
);

public record AchievementBadgeDto(
    int Id,
    string Title,
    string Description,
    string? IconUrl,
    int PointsAwarded,
    DateTimeOffset EarnedAt
);

public record ChartPointDto(
    DateTimeOffset Date,
    decimal Score
);

public record SubmissionDetailDto(
    int Id,
    string MetricName,
    decimal Score,
    decimal Weight,
    DateTimeOffset SubmittedAt,
    string Notes
);

public record PerformanceOverviewDto(
    int EmployeeId,
    DateRangeResolvedDto DateRangeResolved,
    decimal OverallWeightedScore,
    string RatingLabel,
    TasksSummaryDto TasksSummary,
    List<AchievementBadgeDto> Achievements,
    List<ChartPointDto> ChartTrendPoints,
    List<SubmissionDetailDto> SubmissionsDetail
);
