namespace Buy2.Application.DTOs.Points.DTOs;

public record AutomationJobResultDto(
    string ExecutionId,
    DateTimeOffset ExecutedAt,
    int TotalEmployeesEvaluated,
    int TotalPointsAwarded,
    int TotalPointsDeducted,
    int TransactionsCreatedCount,
    List<int> SkippedEmployeeIds,
    List<EmployeeAutomationFailureDto> EmployeeFailures,
    List<EmployeeAutomationSuccessDto> SuccessfulEvaluations,
    string? SummaryNotes
);

public record EmployeeAutomationSuccessDto(
    int EmployeeId,
    string Category,
    int PointsAmount,
    int TransactionId,
    DateTimeOffset EvaluationPeriodStart,
    DateTimeOffset EvaluationPeriodEnd,
    List<RuleEvaluationDetailDto> RuleEvaluations
);

public record EmployeeAutomationFailureDto(
    int EmployeeId,
    string Category,
    string ErrorMessage
);

public record RuleEvaluationDetailDto(
    string RuleType,
    string MetricEvaluated,
    decimal CalculatedValue,
    string MatchedRange,
    int RewardOrDeduction,
    string? TaskPriority,
    int? OverdueDays,
    string Source
);