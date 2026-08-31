namespace Buy2.Application.DTOs.Points.DTOs;

public record AutomationJobResultDto(
    string ExecutionId,
    DateTimeOffset ExecutedAt,
    int TotalEmployeesEvaluated,
    int TotalPointsAwarded,
    int TotalPointsDeducted,
    int TransactionsCreatedCount,
    string? SummaryNotes
);