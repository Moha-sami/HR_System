using Buy2.Domain.Enums;

namespace Buy2.Domain.Permissions;

/// <summary>
/// Domain catalog providing the supported actions and access scopes for each permission module.
/// </summary>
public static class ModuleActionCatalog
{
    private static readonly Dictionary<PermissionModule, IReadOnlyList<string>> ActionsByModule = new()
    {
        [PermissionModule.EmployeeManagement] = new[] { "Add", "Edit", "Delete", "Suspend", "AdminAccess" },
        [PermissionModule.JobManagement] = new[] { "Add", "Edit", "Delete", "View" },
        [PermissionModule.SiteManagement] = new[] { "Add", "Edit", "Delete", "ManageShifts" },
        [PermissionModule.PointsManagement] = new[] { "AddTransaction", "Automation", "ViewTransactions" },
        [PermissionModule.NotificationsManagement] = new[] { "SendNotification" },
        [PermissionModule.RewardManagement] = new[] { "Add", "Edit", "Delete", "ManageInventory" }
    };

    private static readonly Dictionary<PermissionModule, IReadOnlyList<AccessScope>> ScopesByModule = new()
    {
        [PermissionModule.EmployeeManagement] = new[] { AccessScope.All, AccessScope.Department, AccessScope.Region, AccessScope.Site, AccessScope.Team, AccessScope.Specific },
        [PermissionModule.JobManagement] = new[] { AccessScope.All },
        [PermissionModule.SiteManagement] = new[] { AccessScope.All, AccessScope.Region, AccessScope.Site, AccessScope.Specific },
        [PermissionModule.PointsManagement] = new[] { AccessScope.All },
        [PermissionModule.NotificationsManagement] = new[] { AccessScope.All },
        [PermissionModule.RewardManagement] = new[] { AccessScope.All }
    };

    /// <summary>
    /// Gets all supported actions mapped by permission module.
    /// </summary>
    public static IReadOnlyDictionary<PermissionModule, IReadOnlyList<string>> SupportedActionsMap => ActionsByModule;

    /// <summary>
    /// Gets all supported access scopes mapped by permission module.
    /// </summary>
    public static IReadOnlyDictionary<PermissionModule, IReadOnlyList<AccessScope>> SupportedScopesMap => ScopesByModule;

    /// <summary>
    /// Returns the collection of supported action names for a given permission module.
    /// </summary>
    public static IReadOnlyCollection<string> GetSupportedActions(PermissionModule module)
    {
        if (ActionsByModule.TryGetValue(module, out var actions))
        {
            return actions;
        }

        return Array.Empty<string>();
    }

    /// <summary>
    /// Determines whether the specified action is valid and supported for the given permission module.
    /// Comparison is case-insensitive.
    /// </summary>
    public static bool IsActionSupported(PermissionModule module, string action)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return false;
        }

        if (ActionsByModule.TryGetValue(module, out var actions))
        {
            return actions.Any(a => string.Equals(a, action.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    /// <summary>
    /// Returns the collection of supported access scopes for a given permission module.
    /// </summary>
    public static IReadOnlyCollection<AccessScope> GetSupportedScopes(PermissionModule module)
    {
        if (ScopesByModule.TryGetValue(module, out var scopes))
        {
            return scopes;
        }

        return Array.Empty<AccessScope>();
    }

    /// <summary>
    /// Determines whether the specified access scope is valid and supported for the given permission module.
    /// </summary>
    public static bool IsScopeSupported(PermissionModule module, AccessScope scope)
    {
        if (ScopesByModule.TryGetValue(module, out var scopes))
        {
            return scopes.Contains(scope);
        }

        return false;
    }
}
