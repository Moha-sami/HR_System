namespace Buy2.Domain.Entities;

public class TaskAssignment : BaseEntity
{
    public int TaskListId { get; set; }
    public int EmployeeId { get; set; }
    public string Role { get; set; } = string.Empty;

    public TaskList? TaskList { get; set; }
    public Employee? Employee { get; set; }
}
