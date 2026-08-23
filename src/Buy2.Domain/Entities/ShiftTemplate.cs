namespace Buy2.Domain.Entities;

public class ShiftTemplate : BaseEntity
{
    public int? OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string DaysOfWeekJson { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int RequiredHeadcount { get; set; }

    public Organization? Organization { get; set; }
}
