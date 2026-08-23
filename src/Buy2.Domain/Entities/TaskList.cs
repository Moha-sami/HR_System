namespace Buy2.Domain.Entities;

public class TaskList : BaseEntity
{
    public int? OrganizationId { get; set; }
    public int CreatedById { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public Organization? Organization { get; set; }
    public Employee? CreatedBy { get; set; }
    public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
}
