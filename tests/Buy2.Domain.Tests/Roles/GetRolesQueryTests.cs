using Buy2.Application.DTOs.Roles;
using Buy2.Application.Features.Roles.GetRoles;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Roles;

public class GetRolesQueryTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task Handle_DefaultFilter_OrdersByIsSystemRoleDescThenNameAsc()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);

        var role1 = new Role { Name = "Manager", IsSystemRole = false, IsActive = true, PermissionsJson = "[\"manage_schedules\"]" };
        var role2 = new Role { Name = "SuperAdmin", IsSystemRole = true, IsActive = true, PermissionsJson = "[\"all\"]" };
        var role3 = new Role { Name = "Admin", IsSystemRole = true, IsActive = true, PermissionsJson = "[\"all\"]" };
        var role4 = new Role { Name = "Analyst", IsSystemRole = false, IsActive = true, PermissionsJson = "[\"view_reports\"]" };

        context.Roles.AddRange(role1, role2, role3, role4);
        await context.SaveChangesAsync();

        var handler = new GetRolesQueryHandler(repository);
        var query = new GetRolesQuery(new RoleFilterQueryDto(PageNumber: 1, PageSize: 10));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(4, result.Items.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalPages);

        // Order check: System roles first (Admin, SuperAdmin), then non-system roles (Analyst, Manager)
        Assert.True(result.Items[0].IsSystemRole);
        Assert.True(result.Items[1].IsSystemRole);
        Assert.False(result.Items[2].IsSystemRole);
        Assert.False(result.Items[3].IsSystemRole);

        Assert.Equal("Admin", result.Items[0].Name);
        Assert.Equal("SuperAdmin", result.Items[1].Name);
        Assert.Equal("Analyst", result.Items[2].Name);
        Assert.Equal("Manager", result.Items[3].Name);
    }

    [Fact]
    public async Task Handle_SearchTermFilter_MatchesNameAndDescription()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);

        var role1 = new Role { Name = "HR Specialist", Description = "Recruitment and onboarding", IsActive = true };
        var role2 = new Role { Name = "Payroll Lead", Description = "Processes monthly payroll", IsActive = true };
        var role3 = new Role { Name = "Supervisor", Description = "Oversees shift operations and onboarding", IsActive = true };

        context.Roles.AddRange(role1, role2, role3);
        await context.SaveChangesAsync();

        var handler = new GetRolesQueryHandler(repository);
        var query = new GetRolesQuery(new RoleFilterQueryDto(SearchTerm: "onboarding"));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, r => r.Name == "HR Specialist");
        Assert.Contains(result.Items, r => r.Name == "Supervisor");
        Assert.DoesNotContain(result.Items, r => r.Name == "Payroll Lead");
    }

    [Fact]
    public async Task Handle_IsActiveFilter_FiltersByActiveStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);

        var role1 = new Role { Name = "ActiveRole", IsActive = true };
        var role2 = new Role { Name = "DeprecatedRole", IsActive = false };

        context.Roles.AddRange(role1, role2);
        await context.SaveChangesAsync();

        var handler = new GetRolesQueryHandler(repository);
        var activeQuery = new GetRolesQuery(new RoleFilterQueryDto(IsActive: true));
        var inactiveQuery = new GetRolesQuery(new RoleFilterQueryDto(IsActive: false));

        // Act
        var activeResult = await handler.Handle(activeQuery, CancellationToken.None);
        var inactiveResult = await handler.Handle(inactiveQuery, CancellationToken.None);

        // Assert
        Assert.Single(activeResult.Items);
        Assert.Equal("ActiveRole", activeResult.Items[0].Name);

        Assert.Single(inactiveResult.Items);
        Assert.Equal("DeprecatedRole", inactiveResult.Items[0].Name);
    }

    [Fact]
    public async Task Handle_ClampsPageParameters()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);

        context.Roles.Add(new Role { Name = "RoleA" });
        await context.SaveChangesAsync();

        var handler = new GetRolesQueryHandler(repository);
        var query = new GetRolesQuery(new RoleFilterQueryDto(PageNumber: -5, PageSize: 500));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    public async Task Handle_CountsOnlyNonDeletedEmployees()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);

        var role = new Role { Name = "Team Lead" };
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var emp1 = new Employee { FirstName = "John", LastName = "Doe", RoleId = role.Id, IsDeleted = false };
        var emp2 = new Employee { FirstName = "Jane", LastName = "Smith", RoleId = role.Id, IsDeleted = false };
        var emp3 = new Employee { FirstName = "Old", LastName = "Staff", RoleId = role.Id, IsDeleted = true, DeletedAt = DateTimeOffset.UtcNow };

        context.Employees.AddRange(emp1, emp2, emp3);
        await context.SaveChangesAsync();

        var handler = new GetRolesQueryHandler(repository);
        var query = new GetRolesQuery(new RoleFilterQueryDto());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].AssignedEmployeesCount);
    }

    [Fact]
    public async Task Handle_ParsesPermissionsSummary_Correctly()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);

        var roleArray = new Role { Name = "ArrayRole", PermissionsJson = "[\"claim_shifts\", \"view_schedules\"]" };
        var roleEmpty = new Role { Name = "EmptyRole", PermissionsJson = "" };

        context.Roles.AddRange(roleArray, roleEmpty);
        await context.SaveChangesAsync();

        var handler = new GetRolesQueryHandler(repository);
        var query = new GetRolesQuery(new RoleFilterQueryDto());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var arrayDto = result.Items.First(r => r.Name == "ArrayRole");
        Assert.Equal(new List<string> { "claim_shifts", "view_schedules" }, arrayDto.PermissionsSummary);

        var emptyDto = result.Items.First(r => r.Name == "EmptyRole");
        Assert.Empty(emptyDto.PermissionsSummary);
    }
}
