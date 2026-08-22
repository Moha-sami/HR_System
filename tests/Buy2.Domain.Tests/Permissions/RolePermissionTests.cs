using Buy2.Domain.Enums;
using Buy2.Domain.Exceptions;
using Buy2.Domain.Permissions;
using Buy2.Domain.ValueObjects;
using Xunit;

namespace Buy2.Domain.Tests.Permissions;

public class RolePermissionTests
{
    [Fact]
    public void ModuleActionCatalog_ContainsAllDefinedModules()
    {
        // Assert catalog maps all 6 permission modules
        Assert.Equal(6, ModuleActionCatalog.SupportedActionsMap.Count);
        
        var employeeActions = ModuleActionCatalog.GetSupportedActions(PermissionModule.EmployeeManagement);
        Assert.Equal(new[] { "Add", "Edit", "Delete", "Suspend", "AdminAccess" }, employeeActions);

        var jobActions = ModuleActionCatalog.GetSupportedActions(PermissionModule.JobManagement);
        Assert.Equal(new[] { "Add", "Edit", "Delete", "View" }, jobActions);

        var siteActions = ModuleActionCatalog.GetSupportedActions(PermissionModule.SiteManagement);
        Assert.Equal(new[] { "Add", "Edit", "Delete", "ManageShifts" }, siteActions);

        var pointsActions = ModuleActionCatalog.GetSupportedActions(PermissionModule.PointsManagement);
        Assert.Equal(new[] { "AddTransaction", "Automation", "ViewTransactions" }, pointsActions);

        var notificationActions = ModuleActionCatalog.GetSupportedActions(PermissionModule.NotificationsManagement);
        Assert.Equal(new[] { "SendNotification" }, notificationActions);

        var rewardActions = ModuleActionCatalog.GetSupportedActions(PermissionModule.RewardManagement);
        Assert.Equal(new[] { "Add", "Edit", "Delete", "ManageInventory" }, rewardActions);
    }

    [Theory]
    [InlineData(PermissionModule.EmployeeManagement, "add", true)]
    [InlineData(PermissionModule.EmployeeManagement, "EDIT", true)]
    [InlineData(PermissionModule.EmployeeManagement, "Delete", true)]
    [InlineData(PermissionModule.EmployeeManagement, "Suspend", true)]
    [InlineData(PermissionModule.EmployeeManagement, "AdminAccess", true)]
    [InlineData(PermissionModule.EmployeeManagement, "UnknownAction", false)]
    [InlineData(PermissionModule.JobManagement, "View", true)]
    [InlineData(PermissionModule.JobManagement, "Suspend", false)]
    [InlineData(PermissionModule.SiteManagement, "ManageShifts", true)]
    [InlineData(PermissionModule.PointsManagement, "Automation", true)]
    [InlineData(PermissionModule.NotificationsManagement, "SendNotification", true)]
    [InlineData(PermissionModule.RewardManagement, "ManageInventory", true)]
    public void ModuleActionCatalog_IsActionSupported_ValidatesCorrectly(PermissionModule module, string action, bool expected)
    {
        var result = ModuleActionCatalog.IsActionSupported(module, action);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ModulePermission_ValidGlobalPermission_CreatesSuccessfully()
    {
        var perm = new ModulePermission(
            PermissionModule.EmployeeManagement,
            new[] { "Add", "Edit" },
            AccessScope.All,
            null);

        Assert.Equal(PermissionModule.EmployeeManagement, perm.Module);
        Assert.Equal(2, perm.Actions.Count);
        Assert.Equal(AccessScope.All, perm.Scope);
        Assert.Empty(perm.ScopeTargetIds);
        Assert.True(perm.HasAction("add"));
        Assert.True(perm.HasAction("EDIT"));
        Assert.False(perm.HasAction("Delete"));
    }

    [Fact]
    public void ModulePermission_ValidScopedPermission_CreatesSuccessfully()
    {
        var perm = new ModulePermission(
            PermissionModule.SiteManagement,
            new[] { "Edit", "ManageShifts" },
            AccessScope.Site,
            new[] { 101, 102 });

        Assert.Equal(PermissionModule.SiteManagement, perm.Module);
        Assert.Equal(AccessScope.Site, perm.Scope);
        Assert.Equal(new[] { 101, 102 }, perm.ScopeTargetIds);
        Assert.True(perm.HasScopeAccess(AccessScope.Site, 101));
        Assert.True(perm.HasScopeAccess(AccessScope.Site, 102));
        Assert.False(perm.HasScopeAccess(AccessScope.Site, 999));
        Assert.False(perm.HasScopeAccess(AccessScope.Department, 101));
        Assert.False(perm.HasScopeAccess(AccessScope.All));
    }

    [Fact]
    public void ModulePermission_ThrowsWhenActionsNullOrEmpty()
    {
        Assert.Throws<InvalidRolePermissionException>(() =>
            new ModulePermission(PermissionModule.EmployeeManagement, null));

        Assert.Throws<InvalidRolePermissionException>(() =>
            new ModulePermission(PermissionModule.EmployeeManagement, Array.Empty<string>()));

        Assert.Throws<InvalidRolePermissionException>(() =>
            new ModulePermission(PermissionModule.EmployeeManagement, new[] { " ", "" }));
    }

    [Fact]
    public void ModulePermission_ThrowsWhenActionIsUnsupported()
    {
        var ex = Assert.Throws<InvalidRolePermissionException>(() =>
            new ModulePermission(PermissionModule.JobManagement, new[] { "Add", "InvalidAction" }));

        Assert.Contains("InvalidAction", ex.Message);
    }

    [Fact]
    public void ModulePermission_ThrowsWhenScopeIsAll_AndTargetIdsNotEmpty()
    {
        var ex = Assert.Throws<InvalidRolePermissionException>(() =>
            new ModulePermission(
                PermissionModule.EmployeeManagement,
                new[] { "Add" },
                AccessScope.All,
                new[] { 1, 2, 3 }));

        Assert.Contains("ScopeTargetIds must be empty when Scope is AccessScope.All", ex.Message);
    }

    [Fact]
    public void ModulePermission_ThrowsWhenScopeNotAll_AndTargetIdsEmpty()
    {
        var ex = Assert.Throws<InvalidRolePermissionException>(() =>
            new ModulePermission(
                PermissionModule.EmployeeManagement,
                new[] { "Add" },
                AccessScope.Department,
                Array.Empty<int>()));

        Assert.Contains("ScopeTargetIds must contain at least one ID", ex.Message);
    }

    [Fact]
    public void ModulePermission_ThrowsWhenScopeTargetIdIsZeroOrNegative()
    {
        Assert.Throws<InvalidRolePermissionException>(() =>
            new ModulePermission(
                PermissionModule.EmployeeManagement,
                new[] { "Add" },
                AccessScope.Department,
                new[] { 0 }));

        Assert.Throws<InvalidRolePermissionException>(() =>
            new ModulePermission(
                PermissionModule.EmployeeManagement,
                new[] { "Add" },
                AccessScope.Department,
                new[] { -5 }));
    }

    [Fact]
    public void ModulePermission_ThrowsWhenScopeNotSupportedByModule()
    {
        // JobManagement only supports AccessScope.All
        var ex = Assert.Throws<InvalidRolePermissionException>(() =>
            new ModulePermission(
                PermissionModule.JobManagement,
                new[] { "Add" },
                AccessScope.Department,
                new[] { 1 }));

        Assert.Contains("Access scope 'Department' is not supported for module 'JobManagement'", ex.Message);
    }

    [Fact]
    public void RolePermissionsDocument_ThrowsOnDuplicateModules()
    {
        var perm1 = ModulePermission.CreateGlobal(PermissionModule.EmployeeManagement, "Add");
        var perm2 = ModulePermission.CreateGlobal(PermissionModule.EmployeeManagement, "Edit");

        var ex = Assert.Throws<InvalidRolePermissionException>(() =>
            new RolePermissionsDocument(new[] { perm1, perm2 }));

        Assert.Contains("Duplicate permission module", ex.Message);
    }

    [Fact]
    public void RolePermissionsDocument_HasPermission_EvaluatesGlobalAndScopedCorrectly()
    {
        var doc = RolePermissionsDocument.Create(
            ModulePermission.CreateGlobal(PermissionModule.EmployeeManagement, "Add", "Edit", "Delete"),
            ModulePermission.CreateScoped(PermissionModule.SiteManagement, AccessScope.Site, new[] { 10, 20 }, "ManageShifts")
        );

        // Global EmployeeManagement checks
        Assert.True(doc.HasPermission(PermissionModule.EmployeeManagement, "Add"));
        Assert.True(doc.HasPermission(PermissionModule.EmployeeManagement, "add")); // Case-insensitive
        Assert.True(doc.HasPermission(PermissionModule.EmployeeManagement, "Edit", AccessScope.Department, targetId: 5)); // Global covers all scopes/targets
        Assert.False(doc.HasPermission(PermissionModule.EmployeeManagement, "Suspend")); // Action not granted
        Assert.False(doc.HasPermission(PermissionModule.JobManagement, "View")); // Module not configured

        // Scoped SiteManagement checks
        Assert.True(doc.HasPermission(PermissionModule.SiteManagement, "ManageShifts"));
        Assert.True(doc.HasPermission(PermissionModule.SiteManagement, "ManageShifts", AccessScope.Site, targetId: 10));
        Assert.True(doc.HasPermission(PermissionModule.SiteManagement, "ManageShifts", AccessScope.Site, targetId: 20));
        Assert.False(doc.HasPermission(PermissionModule.SiteManagement, "ManageShifts", AccessScope.Site, targetId: 30)); // Target not granted
        Assert.False(doc.HasPermission(PermissionModule.SiteManagement, "ManageShifts", AccessScope.Department, targetId: 10)); // Scope mismatch
        Assert.False(doc.HasPermission(PermissionModule.SiteManagement, "ManageShifts", AccessScope.All)); // Scoped cannot satisfy All
        Assert.False(doc.HasPermission(PermissionModule.SiteManagement, "Delete", AccessScope.Site, targetId: 10)); // Action not granted
    }

    [Fact]
    public void RolePermissionsDocument_JsonSerialization_RoundtripsSuccessfully()
    {
        var doc = RolePermissionsDocument.Create(
            ModulePermission.CreateGlobal(PermissionModule.EmployeeManagement, "Add", "Edit"),
            ModulePermission.CreateScoped(PermissionModule.SiteManagement, AccessScope.Site, new[] { 1, 2, 3 }, "ManageShifts")
        );

        var json = doc.ToJson();
        Assert.NotNull(json);
        Assert.NotEmpty(json);

        var deserialized = RolePermissionsDocument.FromJson(json);
        Assert.NotNull(deserialized);
        Assert.Equal(doc, deserialized);
        Assert.True(deserialized.HasPermission(PermissionModule.EmployeeManagement, "Add"));
        Assert.True(deserialized.HasPermission(PermissionModule.SiteManagement, "ManageShifts", AccessScope.Site, 2));
        Assert.False(deserialized.HasPermission(PermissionModule.SiteManagement, "ManageShifts", AccessScope.Site, 99));
    }

    [Fact]
    public void RolePermissionsDocument_FromJson_HandlesEmptyOrNull()
    {
        Assert.Empty(RolePermissionsDocument.FromJson(null).Permissions);
        Assert.Empty(RolePermissionsDocument.FromJson("").Permissions);
        Assert.Empty(RolePermissionsDocument.FromJson("   ").Permissions);
        Assert.Empty(RolePermissionsDocument.FromJson("[]").Permissions);
        Assert.Empty(RolePermissionsDocument.FromJson("{}").Permissions);
    }

    [Fact]
    public void RoleEntity_GetSetPermissionsDocument_WorksCorrectly()
    {
        var role = new Domain.Entities.Role
        {
            Name = "HR Manager",
            Description = "Manages employee records"
        };

        var doc = RolePermissionsDocument.Create(
            ModulePermission.CreateGlobal(PermissionModule.EmployeeManagement, "Add", "Edit", "Suspend")
        );

        role.SetPermissionsDocument(doc);
        Assert.NotEmpty(role.PermissionsJson);

        var retrievedDoc = role.GetPermissionsDocument();
        Assert.Equal(doc, retrievedDoc);
        Assert.True(role.HasPermission(PermissionModule.EmployeeManagement, "Edit"));
        Assert.False(role.HasPermission(PermissionModule.SiteManagement, "Add"));
    }
}
