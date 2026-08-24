using Buy2.Application.DTOs.Roles;
using Buy2.Application.Features.Roles.DeleteRole;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Roles;

public class ReassignUsersAndDeleteRoleCommandTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task Handle_RoleWithNoEmployees_DeletesRoleSuccessfully()
    {
        // Arrange
        using var context = CreateDbContext();
        var roleRepository = new GenericRepository<Role>(context);
        var employeeRepository = new GenericRepository<Employee>(context);
        var unitOfWork = new UnitOfWork(context);

        var role = new Role { Id = 10, Name = "EmptyRole", IsSystemRole = false, IsActive = true, PermissionsJson = "[]" };
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var dto = new ReassignUsersAndDeleteRoleDto(DefaultNewRoleId: 2);
        var command = new ReassignUsersAndDeleteRoleCommand(10, dto);
        var handler = new ReassignUsersAndDeleteRoleCommandHandler(roleRepository, employeeRepository, unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(10, result.DeletedRoleId);
        Assert.Equal(0, result.ReassignedEmployeesCount);
        Assert.Equal("Role deleted and users reassigned successfully.", result.Message);

        var savedRole = await context.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == 10);
        Assert.NotNull(savedRole);
        Assert.False(savedRole.IsActive);
    }

    [Fact]
    public async Task Handle_RoleWithAssignedEmployees_ReassignsAndDeletesSuccessfully()
    {
        // Arrange
        using var context = CreateDbContext();
        var roleRepository = new GenericRepository<Role>(context);
        var employeeRepository = new GenericRepository<Employee>(context);
        var unitOfWork = new UnitOfWork(context);

        var oldRole = new Role { Id = 10, Name = "OldRole", IsSystemRole = false, IsActive = true, PermissionsJson = "[]" };
        var defaultTargetRole = new Role { Id = 20, Name = "DefaultRole", IsSystemRole = false, IsActive = true, PermissionsJson = "[]" };
        var mappedTargetRole = new Role { Id = 30, Name = "MappedRole", IsSystemRole = false, IsActive = true, PermissionsJson = "[]" };

        context.Roles.AddRange(oldRole, defaultTargetRole, mappedTargetRole);

        var emp1 = new Employee { Id = 1, FirstName = "John", LastName = "Doe", RoleId = 10, IsDeleted = false };
        var emp2 = new Employee { Id = 2, FirstName = "Jane", LastName = "Smith", RoleId = 10, IsDeleted = false };
        context.Employees.AddRange(emp1, emp2);

        await context.SaveChangesAsync();

        var dto = new ReassignUsersAndDeleteRoleDto(
            DefaultNewRoleId: 20,
            Reassignments: new List<EmployeeReassignmentDto>
            {
                new EmployeeReassignmentDto(1, 30)
            }
        );

        var command = new ReassignUsersAndDeleteRoleCommand(10, dto);
        var handler = new ReassignUsersAndDeleteRoleCommandHandler(roleRepository, employeeRepository, unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(10, result.DeletedRoleId);
        Assert.Equal(2, result.ReassignedEmployeesCount);

        var updatedEmp1 = await context.Employees.FindAsync(1);
        var updatedEmp2 = await context.Employees.FindAsync(2);

        Assert.NotNull(updatedEmp1);
        Assert.Equal(30, updatedEmp1.RoleId);

        Assert.NotNull(updatedEmp2);
        Assert.Equal(20, updatedEmp2.RoleId);

        var savedOldRole = await context.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == 10);
        Assert.NotNull(savedOldRole);
        Assert.False(savedOldRole.IsActive);
    }

    [Fact]
    public async Task Handle_SystemRole_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var roleRepository = new GenericRepository<Role>(context);
        var employeeRepository = new GenericRepository<Employee>(context);
        var unitOfWork = new UnitOfWork(context);

        var systemRole = new Role { Id = 1, Name = "Admin", IsSystemRole = true, IsActive = true, PermissionsJson = "[]" };
        context.Roles.Add(systemRole);
        await context.SaveChangesAsync();

        var dto = new ReassignUsersAndDeleteRoleDto(DefaultNewRoleId: 2);
        var command = new ReassignUsersAndDeleteRoleCommand(1, dto);
        var handler = new ReassignUsersAndDeleteRoleCommandHandler(roleRepository, employeeRepository, unitOfWork);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("System roles cannot be deleted.", ex.Message);
    }

    [Fact]
    public async Task Handle_NonExistentRole_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var roleRepository = new GenericRepository<Role>(context);
        var employeeRepository = new GenericRepository<Employee>(context);
        var unitOfWork = new UnitOfWork(context);

        var dto = new ReassignUsersAndDeleteRoleDto(DefaultNewRoleId: 2);
        var command = new ReassignUsersAndDeleteRoleCommand(999, dto);
        var handler = new ReassignUsersAndDeleteRoleCommandHandler(roleRepository, employeeRepository, unitOfWork);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Role with ID 999 was not found.", ex.Message);
    }

    [Fact]
    public async Task Handle_UnmappedEmployees_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateDbContext();
        var roleRepository = new GenericRepository<Role>(context);
        var employeeRepository = new GenericRepository<Employee>(context);
        var unitOfWork = new UnitOfWork(context);

        var role = new Role { Id = 10, Name = "OldRole", IsSystemRole = false, IsActive = true, PermissionsJson = "[]" };
        context.Roles.Add(role);

        var emp = new Employee { Id = 1, FirstName = "John", LastName = "Doe", RoleId = 10, IsDeleted = false };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var dto = new ReassignUsersAndDeleteRoleDto(DefaultNewRoleId: null, Reassignments: null);
        var command = new ReassignUsersAndDeleteRoleCommand(10, dto);
        var handler = new ReassignUsersAndDeleteRoleCommandHandler(roleRepository, employeeRepository, unitOfWork);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("All assigned employees must be mapped to a replacement role before deletion.", ex.Message);
    }

    [Fact]
    public async Task Handle_InvalidReplacementRoleId_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateDbContext();
        var roleRepository = new GenericRepository<Role>(context);
        var employeeRepository = new GenericRepository<Employee>(context);
        var unitOfWork = new UnitOfWork(context);

        var oldRole = new Role { Id = 10, Name = "OldRole", IsSystemRole = false, IsActive = true, PermissionsJson = "[]" };
        var inactiveRole = new Role { Id = 20, Name = "InactiveRole", IsSystemRole = false, IsActive = false, PermissionsJson = "[]" };
        context.Roles.AddRange(oldRole, inactiveRole);

        var emp = new Employee { Id = 1, FirstName = "John", LastName = "Doe", RoleId = 10, IsDeleted = false };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var dto = new ReassignUsersAndDeleteRoleDto(DefaultNewRoleId: 20);
        var command = new ReassignUsersAndDeleteRoleCommand(10, dto);
        var handler = new ReassignUsersAndDeleteRoleCommandHandler(roleRepository, employeeRepository, unitOfWork);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid replacement role specified.", ex.Message);
    }

    [Fact]
    public async Task Handle_TargetRoleSameAsDeletedRole_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateDbContext();
        var roleRepository = new GenericRepository<Role>(context);
        var employeeRepository = new GenericRepository<Employee>(context);
        var unitOfWork = new UnitOfWork(context);

        var oldRole = new Role { Id = 10, Name = "OldRole", IsSystemRole = false, IsActive = true, PermissionsJson = "[]" };
        context.Roles.Add(oldRole);

        var emp = new Employee { Id = 1, FirstName = "John", LastName = "Doe", RoleId = 10, IsDeleted = false };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var dto = new ReassignUsersAndDeleteRoleDto(DefaultNewRoleId: 10);
        var command = new ReassignUsersAndDeleteRoleCommand(10, dto);
        var handler = new ReassignUsersAndDeleteRoleCommandHandler(roleRepository, employeeRepository, unitOfWork);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid replacement role specified.", ex.Message);
    }
}
