namespace Buy2.Domain.Entities;

public class ShiftBlock : BaseEntity
{
    public int ShiftTemplateId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int JobRoleId { get; set; }
    public int? EmployeeId { get; set; }

    public ShiftTemplate? ShiftTemplate { get; set; }
    public JobRole? JobRole { get; set; }
    public Employee? Employee { get; set; }
}
