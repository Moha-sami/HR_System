using Buy2.Domain.Enums;

namespace Buy2.Domain.Entities;

public class PointsTransaction : BaseEntity
{
    public int EmployeeId { get; set; }
    public int? PointsRuleId { get; set; }
    public int Amount { get; set; }
    public TransactionType TransactionType { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTimeOffset? EvaluationPeriodStart { get; set; }
    public DateTimeOffset? EvaluationPeriodEnd { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public PointsRule? PointsRule { get; set; }
}