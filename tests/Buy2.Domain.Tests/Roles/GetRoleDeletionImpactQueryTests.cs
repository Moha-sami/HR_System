using Buy2.Application.DTOs.Roles;
using Buy2.Application.Features.Roles.GetRoleDeletionImpact;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Roles;

public class GetRoleDeletionImpactQueryTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task Handle_RoleWithNoEmployeesAndNotSystemRole_ReturnsCanDeleteDirectlyTrue()
    {
        // Arrange
        using var context = CreateDbContext();
        var roleRepository = new GenericRepository<Role>(context);
        var employeeRepository = new GenericRepository<Employee>(context);

        var role = new Role
        {
            Name = "Custom Role",
            IsSystemRole = false,
            IsActive = true
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var handler = new GetRoleDeletionImpactQueryHandler(roleRepository, employeeRepository);
        var query = new GetRoleDeletionImpactQuery(role.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(role.Id, result.RoleId);
        Assert.Equal("Custom Role", result.RoleName);
        Assert.False(result.IsSystemRole);
        Assert.True(result.CanDeleteDirectly);
        Assert.Equal(0, result.AssignedEmployeesCount);
        Assert.Empty(result.AffectedEmployees);
    }

    [Fact]
    public async Task Handle_RoleWithActiveAssignedEmployees_ReturnsCanDeleteDirectlyFalseAndPopulatesAffectedEmployees()
    {
        // Arrange
        using var context = CreateDbContext();
        var roleRepository = new GenericRepository<Role>(context);
        var employeeRepository = new GenericRepository<Employee>(context);

        var role = new Role
        {
            Name = "Staff Role",
            IsSystemRole = false,
            IsActive = true
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var jobRole = new JobRole { Title = "Software Engineer" };
        var site = new Site { SiteName = "Headquarters" };
        context.JobRoles.Add(jobRole);
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var emp1 = new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            EmployeeCode = "EMP-001",
            RoleId = role.Id,
            JobRoleId = jobRole.Id,
            JobRole = jobRole,
            SiteId = site.Id,
            Site = site,
            IsDeleted = false
        };

        var emp2 = new Employee
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            EmployeeCode = "EMP-002",
            RoleId = role.Id,
            JobRoleId = jobRole.Id,
            JobRole = jobRole,
            SiteId = site.Id,
            Site = site,
            IsDeleted = false
        };

        var empDeleted = new Employee
        {
            FirstName = "Deleted",
            LastName = "User",
            Email = "deleted@example.com",
            EmployeeCode = "EMP-999",
            RoleId = role.Id,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow
        };

        context.Employees.AddRange(emp1, emp2, empDeleted);
        await context.SaveChangesAsync();

        var handler = new GetRoleDeletionImpactQueryHandler(roleRepository, employeeRepository);
        var query = new GetRoleDeletionImpactQuery(role.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(role.Id, result.RoleId);
        Assert.False(result.CanDeleteDirectly);
        Assert.Equal(2, result.AssignedEmployeesCount);
        Assert.Equal(2, result.AffectedEmployees.Count);

        var affectedDoe = result.AffectedEmployees.FirstOrDefault(e => e.EmployeeId == emp1.Id);
        Assert.NotNull(affectedDoe);
        Assert.Equal("EMP-001", affectedDoe.EmployeeCode);
        Assert.Equal("John Doe", affectedDoe.FullName);
        Assert.Equal("john.doe@example.com", affectedDoe.Email);
        Assert.Equal("Software Engineer", affectedDoe.JobRoleTitle);
        Assert.Equal("Headquarters", affectedDoe.SiteName);
    }

    [Fact]
    public async Task Handle_SystemRole_ReturnsCanDeleteDirectlyFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var roleRepository = new GenericRepository<Role>(context);
        var employeeRepository = new GenericRepository<Employee>(context);

        var systemRole = new Role
        {
            Name = "SuperAdmin",
            IsSystemRole = true,
            IsActive = true
        };

        context.Roles.Add(systemRole);
        await context.SaveChangesAsync();

        var handler = new GetRoleDeletionImpactQueryHandler(roleRepository, employeeRepository);
        var query = new GetRoleDeletionImpactQuery(systemRole.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(systemRole.Id, result.RoleId);
        Assert.True(result.IsSystemRole);
        Assert.False(result.CanDeleteDirectly);
        Assert.Equal(0, result.AssignedEmployeesCount);
        Assert.Empty(result.AffectedEmployees);
    }

    [Fact]
    public async Task Handle_NonExistentRoleId_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var roleRepository = new GenericRepository<Role>(context);
        var employeeRepository = new GenericRepository<Employee>(context);

        var handler = new GetRoleDeletionImpactQueryHandler(roleRepository, employeeRepository);
        var query = new GetRoleDeletionImpactQuery(99999);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
