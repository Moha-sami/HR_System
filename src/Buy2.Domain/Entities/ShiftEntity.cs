namespace Buy2.Domain.Entities;

public class ShiftEntity : BaseEntity
{
    public int? EmployeeId { get; set; }
    public int SiteId { get; set; }
    public int JobRoleId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public bool IsPublished { get; set; }
    public int? ShiftTemplateId { get; set; }
    public ShiftTemplate? ShiftTemplate { get; set; }
    public JobRole? JobRole { get; set; }
}