namespace Buy2.Domain.Entities;

public class EmployeeAchievement : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string BadgeType { get; set; } = string.Empty;
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}
