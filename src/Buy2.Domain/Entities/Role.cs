
namespace Buy2.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string PermissionsJson { get; set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation Properties
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

