using Buy2.Domain.Enums;

namespace Buy2.Domain.Entities;

public class EmployeeTask : BaseEntity
{
    public int EmployeeId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public EmployeeTaskStatus Status { get; set; } = EmployeeTaskStatus.Todo;
    public DateTime DueDate { get; set; }

    // Navigation Property
    public Employee? Employee { get; set; }
}
