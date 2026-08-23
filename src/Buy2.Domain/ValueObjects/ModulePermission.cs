using System.Text.Json.Serialization;
using Buy2.Domain.Enums;
using Buy2.Domain.Exceptions;
using Buy2.Domain.Permissions;

namespace Buy2.Domain.ValueObjects;

/// <summary>
/// Value object representing a scoped set of permitted actions within a specific module.
/// Enforces all domain invariants upon construction.
/// </summary>
public sealed class ModulePermission : IEquatable<ModulePermission>
{
    public PermissionModule Module { get; }
    public IReadOnlyList<string> Actions { get; }
    public AccessScope Scope { get; }
    public IReadOnlyList<int> ScopeTargetIds { get; }

    [JsonConstructor]
    public ModulePermission(
        PermissionModule module,
        IReadOnlyList<string>? actions,
        AccessScope scope = AccessScope.All,
        IReadOnlyList<int>? scopeTargetIds = null)
    {
        if (!Enum.IsDefined(typeof(PermissionModule), module))
        {
            throw new InvalidRolePermissionException($"Invalid or undefined permission module '{module}'.");
        }

        if (!Enum.IsDefined(typeof(AccessScope), scope))
        {
            throw new InvalidRolePermissionException($"Invalid or undefined access scope '{scope}'.");
        }

        if (!ModuleActionCatalog.IsScopeSupported(module, scope))
        {
            throw new InvalidRolePermissionException($"Access scope '{scope}' is not supported for module '{module}'.");
        }

        if (actions is null)
        {
            throw new InvalidRolePermissionException($"Actions collection cannot be null for module '{module}'.");
        }

        var normalizedActions = actions
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedActions.Count == 0)
        {
            throw new InvalidRolePermissionException($"At least one action must be specified for module '{module}'.");
        }

        var canonicalActions = new List<string>();
        var supportedActions = ModuleActionCatalog.GetSupportedActions(module);

        foreach (var action in normalizedActions)
        {
            var matchedCanonical = supportedActions.FirstOrDefault(sa => string.Equals(sa, action, StringComparison.OrdinalIgnoreCase));
            if (matchedCanonical is null)
            {
                throw new InvalidRolePermissionException($"Action '{action}' is not supported for module '{module}'. Supported actions: [{string.Join(", ", supportedActions)}].");
            }
            canonicalActions.Add(matchedCanonical);
        }

        var normalizedTargetIds = (scopeTargetIds ?? Enumerable.Empty<int>())
            .Distinct()
            .ToList();

        if (scope == AccessScope.All)
        {
            if (normalizedTargetIds.Count > 0)
            {
                throw new InvalidRolePermissionException($"ScopeTargetIds must be empty when Scope is AccessScope.All for module '{module}'.");
            }
        }
        else
        {
            if (normalizedTargetIds.Count == 0)
            {
                throw new InvalidRolePermissionException($"ScopeTargetIds must contain at least one ID when Scope is '{scope}' for module '{module}'.");
            }

            if (normalizedTargetIds.Any(id => id <= 0))
            {
                throw new InvalidRolePermissionException($"ScopeTargetIds must contain positive integer IDs (> 0) for module '{module}'.");
            }
        }

        Module = module;
        Actions = canonicalActions.AsReadOnly();
        Scope = scope;
        ScopeTargetIds = normalizedTargetIds.AsReadOnly();
    }

    public ModulePermission(
        PermissionModule module,
        IEnumerable<string>? actions,
        AccessScope scope = AccessScope.All,
        IEnumerable<int>? scopeTargetIds = null)
        : this(module, actions?.ToList(), scope, scopeTargetIds?.ToList())
    {
    }

    /// <summary>
    /// Factory method to create a new ModulePermission.
    /// </summary>
    public static ModulePermission Create(
        PermissionModule module,
        IEnumerable<string> actions,
        AccessScope scope = AccessScope.All,
        IEnumerable<int>? scopeTargetIds = null)
    {
        return new ModulePermission(module, actions, scope, scopeTargetIds);
    }

    /// <summary>
    /// Factory method to create a global (All scope) ModulePermission.
    /// </summary>
    public static ModulePermission CreateGlobal(PermissionModule module, params string[] actions)
    {
        return new ModulePermission(module, actions, AccessScope.All, null);
    }

    /// <summary>
    /// Factory method to create a scoped ModulePermission with target entity IDs.
    /// </summary>
    public static ModulePermission CreateScoped(PermissionModule module, AccessScope scope, IEnumerable<int> scopeTargetIds, params string[] actions)
    {
        return new ModulePermission(module, actions, scope, scopeTargetIds);
    }

    /// <summary>
    /// Checks if this permission grant includes the specified action.
    /// </summary>
    public bool HasAction(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return false;
        }

        return Actions.Any(a => string.Equals(a, action.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks whether this module permission covers the specified scope and optional target ID.
    /// </summary>
    public bool HasScopeAccess(AccessScope? requiredScope = null, int? targetId = null)
    {
        if (Scope == AccessScope.All)
        {
            return true;
        }

        if (requiredScope.HasValue)
        {
            if (requiredScope.Value == AccessScope.All)
            {
                return false;
            }

            if (requiredScope.Value != Scope)
            {
                return false;
            }
        }

        if (targetId.HasValue)
        {
            return ScopeTargetIds.Contains(targetId.Value);
        }

        return true;
    }

    public bool Equals(ModulePermission? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Module == other.Module &&
               Scope == other.Scope &&
               Actions.SequenceEqual(other.Actions, StringComparer.OrdinalIgnoreCase) &&
               ScopeTargetIds.SequenceEqual(other.ScopeTargetIds);
    }

    public override bool Equals(object? obj) => Equals(obj as ModulePermission);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Module);
        hash.Add(Scope);
        foreach (var action in Actions)
        {
            hash.Add(action, StringComparer.OrdinalIgnoreCase);
        }
        foreach (var id in ScopeTargetIds)
        {
            hash.Add(id);
        }
        return hash.ToHashCode();
    }
}
