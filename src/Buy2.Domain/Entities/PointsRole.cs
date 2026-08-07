namespace Buy2.Domain.Entities;

public class PointsRule : BaseEntity
{
    public string RuleKey { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public string ConditionExpression { get; set; } = null!;

    public string ActionType { get; set; } = null!;

    public int PointValue { get; set; }
}
