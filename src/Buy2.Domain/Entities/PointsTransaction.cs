namespace Buy2.Domain.Entities;
public class PointsTransaction : BaseEntity
{
    public int EmployeeId { get; set; }
    public int? PointsRuleId { get; set; }
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public virtual Employee Employee { get; set; } = null!;
    public virtual PointsRule? PointsRule { get; set; }
}