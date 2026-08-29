namespace Buy2.Domain.Entities;

public class PointsRule : BaseEntity
{
    public string RuleKey { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ConditionExpression { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public int PointValue { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? UpdatedAt { get; set; }
}