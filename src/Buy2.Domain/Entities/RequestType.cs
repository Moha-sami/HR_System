namespace Buy2.Domain.Entities;

public class RequestType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Hint { get; set; }
    public string? LeaveType { get; set; }
    public string? LeavePay { get; set; }
    public bool RequiresDates { get; set; }
    public bool RequiresReason { get; set; }
    public bool IsActive { get; set; } = true;
    public string? AddedBy { get; set; }
}
