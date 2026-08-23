namespace Buy2.Domain.Entities;

public class Request : BaseEntity
{
    public int EmployeeId { get; set; }
    public int RequestTypeId { get; set; }
    public int? ApproverId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string? RejectionReason { get; set; }

    public Employee? Employee { get; set; }
    public RequestType? RequestType { get; set; }
    public Employee? Approver { get; set; }
}
