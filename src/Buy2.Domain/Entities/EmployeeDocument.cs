namespace Buy2.Domain.Entities;

public class EmployeeDocument : BaseEntity
{
    public int EmployeeId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string StorageUrl { get; set; } = string.Empty;
    public virtual Employee Employee { get; set; } = null!;
}