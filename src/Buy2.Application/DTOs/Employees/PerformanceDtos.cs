using Buy2.Domain.Enums;

namespace Buy2.Application.DTOs.Employees;

// Chart Data Point DTO
public record ChartDataPointDto(
    string Label,
    decimal Score
);

// Achievement DTO
public record AchievementDto(
    string BadgeType,
    DateTime AwardedAt
);

// Metric Score Summary DTO
public record MetricScoreDto(
    int MetricId,
    string MetricName,
    decimal Target,
    decimal Score,
    decimal Weight
);

// Task Item DTO
public record TaskItemDto(
    int Id,
    string TaskName,
    EmployeeTaskStatus Status,
    DateTime DueDate
);

// Task Column Stats DTO
public record TaskColumnStatsDto(
    int Count,
    int Total,
    decimal Percent
);

// Tasks Summary DTO
public record TasksSummaryDto(
    TaskColumnStatsDto Todo,
    TaskColumnStatsDto InProgress,
    TaskColumnStatsDto InReview,
    TaskColumnStatsDto Completed,
    TaskColumnStatsDto Overdue,
    List<TaskItemDto> Tasks
);

// Task Completion Stats DTO
public record TaskCompletionStatsDto(
    decimal DeadlineCompliance,
    decimal CompletionRate,
    decimal AverageDelayDays
);

// Submission DTO for KPI Table
public record PerformanceSubmissionDto(
    decimal AchievedPercent,
    decimal Weight,
    decimal Score,
    DateTime SubmissionDate,
    string Feedback
);

// Metric Detail DTO for KPI Drill-Down Page
public record MetricDetailDto(
    string MetricName,
    string Description,
    decimal Target,
    decimal CurrentScore,
    string ScoreLabel,
    decimal AllTimeAverage,
    List<ChartDataPointDto> ChartDataPoints,
    List<PerformanceSubmissionDto> Submissions
);

// Performance Overview DTO
public record PerformanceOverviewDto(
    decimal PerformanceScore,
    string ScoreLabel,
    List<MetricScoreDto> MetricScores,
    TaskCompletionStatsDto TaskCompletionStats,
    List<AchievementDto> Achievements,
    List<ChartDataPointDto> ChartDataPoints,
    TasksSummaryDto TasksSummary
);
