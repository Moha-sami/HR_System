using Buy2.Application.DTOs.Roles;
using Buy2.Application.Features.Roles.CreateRole;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Roles;

public class CreateRoleCommandTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task Handle_ValidRoleDto_CreatesRoleAndReturnsSuccessResult()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);
        var unitOfWork = new UnitOfWork(context);

        var permissions = new List<ModulePermissionDto>
        {
            new ModulePermissionDto(
                "ShiftManagement",
                new List<string> { "view", "edit" },
                new PermissionScopeDto("Site", new List<int> { 101 })
            )
        };

        var dto = new CreateRoleDto("Shift Lead", "Manages daily shifts", permissions);
        var command = new CreateRoleCommand(dto);
        var handler = new CreateRoleCommandHandler(repository, unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.False(result.IsConflict);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.CreatedRole);
        Assert.True(result.CreatedRole.Id > 0);
        Assert.Equal("Shift Lead", result.CreatedRole.Name);
        Assert.Equal("Manages daily shifts", result.CreatedRole.Description);
        Assert.False(result.CreatedRole.IsSystemRole);
        Assert.True(result.CreatedRole.IsActive);
        Assert.Equal(0, result.CreatedRole.AssignedEmployeesCount);
        Assert.Single(result.CreatedRole.Permissions);
        Assert.Equal("ShiftManagement", result.CreatedRole.Permissions[0].Module);

        // Verify entity state in DB
        var savedRole = await context.Roles.FindAsync(result.CreatedRole.Id);
        Assert.NotNull(savedRole);
        Assert.Equal("Shift Lead", savedRole.Name);
        Assert.False(savedRole.IsSystemRole);
        Assert.True(savedRole.IsActive);
    }

    [Fact]
    public async Task Handle_DuplicateRoleName_ReturnsConflictResult_CaseInsensitive()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);
        var unitOfWork = new UnitOfWork(context);

        var existingRole = new Role
        {
            Name = "Supervisor",
            Description = "Existing role",
            IsSystemRole = false,
            IsActive = true,
            PermissionsJson = "[]"
        };
        context.Roles.Add(existingRole);
        await context.SaveChangesAsync();

        var dto = new CreateRoleDto("  sUpErViSoR  ", "Duplicate role test", new List<ModulePermissionDto>());
        var command = new CreateRoleCommand(dto);
        var handler = new CreateRoleCommandHandler(repository, unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.True(result.IsConflict);
        Assert.Null(result.CreatedRole);
        Assert.Equal("Role with name '  sUpErViSoR  ' already exists.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_NewRole_SystemRolePropertyIsAlwaysFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);
        var unitOfWork = new UnitOfWork(context);

        var dto = new CreateRoleDto("Custom Operational Role", null, null);
        var command = new CreateRoleCommand(dto);
        var handler = new CreateRoleCommandHandler(repository, unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.CreatedRole);
        Assert.False(result.CreatedRole.IsSystemRole);

        var savedRole = await context.Roles.FindAsync(result.CreatedRole.Id);
        Assert.NotNull(savedRole);
        Assert.False(savedRole.IsSystemRole);
    }

    [Fact]
    public async Task Handle_RoleNameWithLeadingTrailingWhitespace_TrimsNameBeforeSaving()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);
        var unitOfWork = new UnitOfWork(context);

        var dto = new CreateRoleDto("  Trimmed Role Name  ", "  Trimmed Description  ", null);
        var command = new CreateRoleCommand(dto);
        var handler = new CreateRoleCommandHandler(repository, unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.CreatedRole);
        Assert.Equal("Trimmed Role Name", result.CreatedRole.Name);
        Assert.Equal("Trimmed Description", result.CreatedRole.Description);

        var savedRole = await context.Roles.FindAsync(result.CreatedRole.Id);
        Assert.NotNull(savedRole);
        Assert.Equal("Trimmed Role Name", savedRole.Name);
        Assert.Equal("Trimmed Description", savedRole.Description);
    }
}
