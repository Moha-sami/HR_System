namespace Buy2.Domain.Entities;

public class DisciplinaryViolation : BaseEntity
{
    public int EmployeeId { get; set; }
    public string Severity { get; set; } = null!;
    public string Description { get; set; } = null!;
    public virtual Employee Employee { get; set; } = null!;
}
