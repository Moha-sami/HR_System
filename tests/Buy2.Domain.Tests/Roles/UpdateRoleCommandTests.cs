using Buy2.Application.DTOs.Roles;
using Buy2.Application.Features.Roles.UpdateRole;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Roles;

public class UpdateRoleCommandTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task Handle_ValidRoleDto_UpdatesRoleAndReturnsSuccessResult()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);
        var unitOfWork = new UnitOfWork(context);

        var existingRole = new Role
        {
            Name = "Original Role",
            Description = "Original description",
            IsSystemRole = false,
            IsActive = true,
            PermissionsJson = "[]",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        context.Roles.Add(existingRole);
        await context.SaveChangesAsync();

        var updatedPermissions = new List<ModulePermissionDto>
        {
            new ModulePermissionDto(
                "LeaveManagement",
                new List<string> { "approve", "reject" },
                new PermissionScopeDto("Department", new List<int> { 1, 2 })
            )
        };

        var dto = new UpdateRoleDto("Updated Role Name", "Updated description", true, updatedPermissions);
        var command = new UpdateRoleCommand(existingRole.Id, dto);
        var handler = new UpdateRoleCommandHandler(repository, unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.False(result.IsNotFound);
        Assert.False(result.IsForbidden);
        Assert.False(result.IsConflict);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.UpdatedRole);
        Assert.Equal(existingRole.Id, result.UpdatedRole.Id);
        Assert.Equal("Updated Role Name", result.UpdatedRole.Name);
        Assert.Equal("Updated description", result.UpdatedRole.Description);
        Assert.True(result.UpdatedRole.IsActive);
        Assert.NotNull(result.UpdatedRole.UpdatedAt);
        Assert.Single(result.UpdatedRole.Permissions);

        // Verify state in DB
        var savedRole = await context.Roles.FindAsync(existingRole.Id);
        Assert.NotNull(savedRole);
        Assert.Equal("Updated Role Name", savedRole.Name);
        Assert.Equal("Updated description", savedRole.Description);
    }

    [Fact]
    public async Task Handle_RoleNotFound_ReturnsNotFoundResult()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);
        var unitOfWork = new UnitOfWork(context);

        var dto = new UpdateRoleDto("Non Existent Role", "Description", true, null);
        var command = new UpdateRoleCommand(999, dto);
        var handler = new UpdateRoleCommandHandler(repository, unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.True(result.IsNotFound);
        Assert.False(result.IsForbidden);
        Assert.False(result.IsConflict);
        Assert.Null(result.UpdatedRole);
        Assert.Equal("Role with ID 999 was not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_SystemRoleNameModification_ReturnsForbiddenResult()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);
        var unitOfWork = new UnitOfWork(context);

        var systemRole = new Role
        {
            Name = "Admin",
            Description = "System admin role",
            IsSystemRole = true,
            IsActive = true,
            PermissionsJson = "[]"
        };
        context.Roles.Add(systemRole);
        await context.SaveChangesAsync();

        var dto = new UpdateRoleDto("Super Admin", "System admin role", true, null);
        var command = new UpdateRoleCommand(systemRole.Id, dto);
        var handler = new UpdateRoleCommandHandler(repository, unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.False(result.IsNotFound);
        Assert.True(result.IsForbidden);
        Assert.False(result.IsConflict);
        Assert.Null(result.UpdatedRole);
        Assert.Equal("Core system role name and active status cannot be modified.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_SystemRoleDeactivation_ReturnsForbiddenResult()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);
        var unitOfWork = new UnitOfWork(context);

        var systemRole = new Role
        {
            Name = "Admin",
            Description = "System admin role",
            IsSystemRole = true,
            IsActive = true,
            PermissionsJson = "[]"
        };
        context.Roles.Add(systemRole);
        await context.SaveChangesAsync();

        var dto = new UpdateRoleDto("Admin", "System admin role", false, null);
        var command = new UpdateRoleCommand(systemRole.Id, dto);
        var handler = new UpdateRoleCommandHandler(repository, unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.False(result.IsNotFound);
        Assert.True(result.IsForbidden);
        Assert.False(result.IsConflict);
        Assert.Null(result.UpdatedRole);
        Assert.Equal("Core system role name and active status cannot be modified.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_SystemRoleDescriptionOrPermissionsUpdate_Allowed()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);
        var unitOfWork = new UnitOfWork(context);

        var systemRole = new Role
        {
            Name = "Admin",
            Description = "Old admin description",
            IsSystemRole = true,
            IsActive = true,
            PermissionsJson = "[]"
        };
        context.Roles.Add(systemRole);
        await context.SaveChangesAsync();

        var dto = new UpdateRoleDto("admin", "Updated admin description", true, null);
        var command = new UpdateRoleCommand(systemRole.Id, dto);
        var handler = new UpdateRoleCommandHandler(repository, unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.UpdatedRole);
        Assert.Equal("admin", result.UpdatedRole.Name);
        Assert.Equal("Updated admin description", result.UpdatedRole.Description);
    }

    [Fact]
    public async Task Handle_DuplicateRoleName_ReturnsConflictResult_CaseInsensitive()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);
        var unitOfWork = new UnitOfWork(context);

        var role1 = new Role { Name = "Manager", Description = "Desc 1", IsActive = true, PermissionsJson = "[]" };
        var role2 = new Role { Name = "Team Lead", Description = "Desc 2", IsActive = true, PermissionsJson = "[]" };
        context.Roles.AddRange(role1, role2);
        await context.SaveChangesAsync();

        var dto = new UpdateRoleDto("  mAnAgEr  ", "Updated description", true, null);
        var command = new UpdateRoleCommand(role2.Id, dto);
        var handler = new UpdateRoleCommandHandler(repository, unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.False(result.IsNotFound);
        Assert.False(result.IsForbidden);
        Assert.True(result.IsConflict);
        Assert.Null(result.UpdatedRole);
        Assert.Equal("Role with name '  mAnAgEr  ' already exists.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_UpdatingSameRoleWithSameName_DoesNotConflict()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);
        var unitOfWork = new UnitOfWork(context);

        var role = new Role { Name = "Manager", Description = "Desc", IsActive = true, PermissionsJson = "[]" };
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var dto = new UpdateRoleDto("Manager", "Updated desc", true, null);
        var command = new UpdateRoleCommand(role.Id, dto);
        var handler = new UpdateRoleCommandHandler(repository, unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.UpdatedRole);
        Assert.Equal("Updated desc", result.UpdatedRole.Description);
    }
}
