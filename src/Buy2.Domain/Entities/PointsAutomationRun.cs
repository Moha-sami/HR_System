using Buy2.Domain.Enums;

namespace Buy2.Domain.Entities;

public class PointsAutomationRun : BaseEntity
{
    public AutomationCategory? Category { get; set; }
    public AutomationPeriod AutomationPeriod { get; set; }
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public AutomationRunStatus Status { get; set; } = AutomationRunStatus.InProgress;
    public int EmployeesEvaluated { get; set; }
    public int TransactionsCreated { get; set; }
    public DateTimeOffset ExecutedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ErrorMessage { get; set; }
}
