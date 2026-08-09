namespace Buy2.Domain.Entities;

public class Shift : BaseEntity
{
    public int EmployeeId { get; set; }
    public int JobRoleId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public bool IsPublished { get; set; }

    public int SiteId { get; set; }
    public virtual Site Site { get; set; } = null!;
}