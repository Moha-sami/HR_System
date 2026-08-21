namespace Buy2.Application.Features.Employees.GetMetricDetail;

public record DateRangeResolvedDto(
    DateTimeOffset From,
    DateTimeOffset To,
    string? Period = null
);

public record MonthlyTrendPointDto(
    int Year,
    int Month,
    string YearMonthLabel,
    decimal AverageScore,
    int SubmissionCount
);

public record MetricSubmissionItemDto(
    int Id,
    decimal Score,
    DateTimeOffset SubmittedAt,
    string Notes,
    string? EvaluatorName
);

public record MetricDetailDto(
    int EmployeeId,
    int MetricId,
    string MetricName,
    string MetricDescription,
    decimal Weight,
    decimal TargetScore,
    string Unit,
    decimal AllTimeAverageScore,
    decimal PeriodAverageScore,
    string PeriodRatingLabel,
    DateRangeResolvedDto DateRangeResolved,
    List<MonthlyTrendPointDto> MonthlyTrends,
    List<MetricSubmissionItemDto> Submissions
);
