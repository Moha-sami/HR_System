namespace Buy2.Domain.Entities;

public class EmployeeAchievement : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int? BadgeId { get; set; }
    public Badge? Badge { get; set; }
    public string BadgeType { get; set; } = string.Empty;
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
    public int PointsAwarded { get; set; }
}
