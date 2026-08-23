using System.Text.Json;
using System.Text.Json.Serialization;
using Buy2.Domain.Enums;
using Buy2.Domain.Exceptions;

namespace Buy2.Domain.ValueObjects;

/// <summary>
/// Value object representing the complete permission document for a role.
/// Enforces uniqueness of modules and validates overall document integrity.
/// </summary>
public sealed class RolePermissionsDocument : IEquatable<RolePermissionsDocument>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public IReadOnlyList<ModulePermission> Permissions { get; }

    /// <summary>
    /// Gets an empty RolePermissionsDocument instance.
    /// </summary>
    public static RolePermissionsDocument Empty => new(Array.Empty<ModulePermission>());

    [JsonConstructor]
    public RolePermissionsDocument(IEnumerable<ModulePermission>? permissions)
    {
        if (permissions is null)
        {
            Permissions = Array.Empty<ModulePermission>();
        }
        else
        {
            Permissions = permissions.ToList().AsReadOnly();
        }

        Validate();
    }

    /// <summary>
    /// Factory method to create a RolePermissionsDocument from a collection of module permissions.
    /// </summary>
    public static RolePermissionsDocument Create(IEnumerable<ModulePermission> permissions)
    {
        return new RolePermissionsDocument(permissions);
    }

    /// <summary>
    /// Factory method to create a RolePermissionsDocument from params of module permissions.
    /// </summary>
    public static RolePermissionsDocument Create(params ModulePermission[] permissions)
    {
        return new RolePermissionsDocument(permissions);
    }

    /// <summary>
    /// Validates the domain invariants for the entire permissions document.
    /// Throws InvalidRolePermissionException if any invariant is violated.
    /// </summary>
    public void Validate()
    {
        if (Permissions is null)
        {
            throw new InvalidRolePermissionException("Permissions collection cannot be null.");
        }

        var duplicateModules = Permissions
            .GroupBy(p => p.Module)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateModules.Count > 0)
        {
            throw new InvalidRolePermissionException($"Duplicate permission module entries found: [{string.Join(", ", duplicateModules)}]. Each module can only be configured once.");
        }
    }

    /// <summary>
    /// Determines whether this permission document grants access to the specified module, action, and optional scope/target.
    /// </summary>
    /// <param name="module">The system functional module.</param>
    /// <param name="action">The action name being requested (case-insensitive).</param>
    /// <param name="scope">The scope level required for the operation (optional).</param>
    /// <param name="targetId">The specific target entity ID being accessed (optional).</param>
    /// <returns>True if access is granted by this document; otherwise, false.</returns>
    public bool HasPermission(PermissionModule module, string action, AccessScope? scope = null, int? targetId = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return false;
        }

        var modulePermission = Permissions.FirstOrDefault(p => p.Module == module);
        if (modulePermission is null)
        {
            return false;
        }

        if (!modulePermission.HasAction(action))
        {
            return false;
        }

        return modulePermission.HasScopeAccess(scope, targetId);
    }

    /// <summary>
    /// Retrieves the ModulePermission configuration for a specific module, or null if not configured.
    /// </summary>
    public ModulePermission? GetModulePermission(PermissionModule module)
    {
        return Permissions.FirstOrDefault(p => p.Module == module);
    }

    /// <summary>
    /// Checks if a specific module is configured in this document.
    /// </summary>
    public bool HasModule(PermissionModule module)
    {
        return Permissions.Any(p => p.Module == module);
    }

    /// <summary>
    /// Serializes this permissions document to JSON.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(Permissions, JsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string into a RolePermissionsDocument, validating domain invariants.
    /// </summary>
    public static RolePermissionsDocument FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Empty;
        }

        var trimmed = json.Trim();
        if (trimmed == "[]" || trimmed == "{}")
        {
            return Empty;
        }

        try
        {
            if (trimmed.StartsWith("["))
            {
                var list = JsonSerializer.Deserialize<List<ModulePermission>>(trimmed, JsonOptions);
                return new RolePermissionsDocument(list);
            }
            else
            {
                var doc = JsonSerializer.Deserialize<RolePermissionsDocument>(trimmed, JsonOptions);
                return doc ?? Empty;
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidRolePermissionException($"Failed to deserialize role permissions JSON: {ex.Message}", ex);
        }
    }

    public bool Equals(RolePermissionsDocument? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (Permissions.Count != other.Permissions.Count) return false;

        var dictThis = Permissions.ToDictionary(p => p.Module);
        foreach (var otherPerm in other.Permissions)
        {
            if (!dictThis.TryGetValue(otherPerm.Module, out var thisPerm) || !thisPerm.Equals(otherPerm))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as RolePermissionsDocument);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var perm in Permissions.OrderBy(p => p.Module))
        {
            hash.Add(perm);
        }
        return hash.ToHashCode();
    }
}
