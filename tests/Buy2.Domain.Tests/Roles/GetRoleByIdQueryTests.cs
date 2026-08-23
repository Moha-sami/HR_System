using Buy2.Application.DTOs.Roles;
using Buy2.Application.Features.Roles.GetRoleById;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Roles;

public class GetRoleByIdQueryTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task Handle_ExistingRoleId_ReturnsRoleDetailsDto_WithAssignedCountAndPermissions()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);

        var permissionsJson = @"[
            {
                ""module"": ""Employees"",
                ""actions"": [""view"", ""edit""],
                ""scope"": { ""scopeType"": ""All"", ""targetIds"": null }
            }
        ]";

        var role = new Role
        {
            Name = "HR Manager",
            Description = "Manages HR activities",
            IsSystemRole = false,
            IsActive = true,
            PermissionsJson = permissionsJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var empActive1 = new Employee { FirstName = "Alice", LastName = "Smith", RoleId = role.Id, IsDeleted = false };
        var empActive2 = new Employee { FirstName = "Bob", LastName = "Jones", RoleId = role.Id, IsDeleted = false };
        var empDeleted = new Employee { FirstName = "Charlie", LastName = "Brown", RoleId = role.Id, IsDeleted = true, DeletedAt = DateTimeOffset.UtcNow };

        context.Employees.AddRange(empActive1, empActive2, empDeleted);
        await context.SaveChangesAsync();

        var handler = new GetRoleByIdQueryHandler(repository);
        var query = new GetRoleByIdQuery(role.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(role.Id, result.Id);
        Assert.Equal("HR Manager", result.Name);
        Assert.Equal("Manages HR activities", result.Description);
        Assert.False(result.IsSystemRole);
        Assert.True(result.IsActive);
        Assert.Equal(2, result.AssignedEmployeesCount);
        Assert.NotNull(result.Permissions);
        Assert.Single(result.Permissions);
        Assert.Equal("Employees", result.Permissions[0].Module);
        Assert.Equal(new List<string> { "view", "edit" }, result.Permissions[0].Actions);
        Assert.Equal("All", result.Permissions[0].Scope?.ScopeType);
    }

    [Fact]
    public async Task Handle_InactiveRole_ReturnsRoleDetailsDto()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);

        var inactiveRole = new Role
        {
            Name = "Deprecated Role",
            Description = "Legacy role no longer active",
            IsSystemRole = false,
            IsActive = false,
            PermissionsJson = "[]"
        };

        context.Roles.Add(inactiveRole);
        await context.SaveChangesAsync();

        var handler = new GetRoleByIdQueryHandler(repository);
        var query = new GetRoleByIdQuery(inactiveRole.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(inactiveRole.Id, result.Id);
        Assert.Equal("Deprecated Role", result.Name);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task Handle_NonExistentRoleId_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);

        var handler = new GetRoleByIdQueryHandler(repository);
        var query = new GetRoleByIdQuery(9999);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_RoleWithLegacyStringArrayPermissions_ParsesCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);

        var role = new Role
        {
            Name = "Shift Supervisor",
            IsActive = true,
            PermissionsJson = @"[""claim_shifts"", ""view_schedules""]"
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var handler = new GetRoleByIdQueryHandler(repository);
        var query = new GetRoleByIdQuery(role.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Permissions.Count);
        Assert.Equal("claim_shifts", result.Permissions[0].Module);
        Assert.Equal("view_schedules", result.Permissions[1].Module);
    }

    [Fact]
    public async Task Handle_RoleWithInvalidOrEmptyPermissionsJson_ReturnsEmptyPermissionsList()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);

        var roleEmpty = new Role { Name = "EmptyPerms", IsActive = true, PermissionsJson = "" };
        var roleNull = new Role { Name = "NullPerms", IsActive = true, PermissionsJson = string.Empty };
        var roleInvalid = new Role { Name = "InvalidPerms", IsActive = true, PermissionsJson = "{ invalid json }" };

        context.Roles.AddRange(roleEmpty, roleNull, roleInvalid);
        await context.SaveChangesAsync();

        var handler = new GetRoleByIdQueryHandler(repository);

        // Act
        var resEmpty = await handler.Handle(new GetRoleByIdQuery(roleEmpty.Id), CancellationToken.None);
        var resNull = await handler.Handle(new GetRoleByIdQuery(roleNull.Id), CancellationToken.None);
        var resInvalid = await handler.Handle(new GetRoleByIdQuery(roleInvalid.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(resEmpty);
        Assert.Empty(resEmpty.Permissions);

        Assert.NotNull(resNull);
        Assert.Empty(resNull.Permissions);

        Assert.NotNull(resInvalid);
        Assert.Empty(resInvalid.Permissions);
    }
}
