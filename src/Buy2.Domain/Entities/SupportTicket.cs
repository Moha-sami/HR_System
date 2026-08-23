namespace Buy2.Domain.Entities;

public class SupportTicket : BaseEntity
{
    public int EmployeeId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ResolvedAt { get; set; }

    public Employee? Employee { get; set; }
}
