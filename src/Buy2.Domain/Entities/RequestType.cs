namespace Buy2.Domain.Entities;

public class RequestType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool RequiresDates { get; set; }
    public bool RequiresReason { get; set; }
}
