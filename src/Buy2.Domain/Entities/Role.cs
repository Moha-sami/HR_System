using Buy2.Domain.Enums;
using Buy2.Domain.ValueObjects;

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

    /// <summary>
    /// Parses and returns the typed RolePermissionsDocument domain model.
    /// </summary>
    public RolePermissionsDocument GetPermissionsDocument() => RolePermissionsDocument.FromJson(PermissionsJson);

    /// <summary>
    /// Updates the PermissionsJson payload from a validated RolePermissionsDocument.
    /// </summary>
    public void SetPermissionsDocument(RolePermissionsDocument document)
    {
        PermissionsJson = document?.ToJson() ?? "[]";
    }

    /// <summary>
    /// Evaluates whether this role grants a specific module action under the given scope/target.
    /// </summary>
    public bool HasPermission(PermissionModule module, string action, AccessScope? scope = null, int? targetId = null)
    {
        return GetPermissionsDocument().HasPermission(module, action, scope, targetId);
    }
}
