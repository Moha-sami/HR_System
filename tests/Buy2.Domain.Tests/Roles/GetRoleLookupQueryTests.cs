using Buy2.Application.DTOs.Roles;
using Buy2.Application.Features.Roles.GetRoleLookup;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Roles;

public class GetRoleLookupQueryTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task Handle_ReturnsActiveRolesOrderedAlphabeticallyByName()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);

        var role1 = new Role { Name = "Manager", IsActive = true };
        var role2 = new Role { Name = "Admin", IsActive = true };
        var role3 = new Role { Name = "Developer", IsActive = true };

        context.Roles.AddRange(role1, role2, role3);
        await context.SaveChangesAsync();

        var handler = new GetRoleLookupQueryHandler(repository);
        var query = new GetRoleLookupQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Admin", result[0].Name);
        Assert.Equal("Developer", result[1].Name);
        Assert.Equal("Manager", result[2].Name);
    }

    [Fact]
    public async Task Handle_ExcludesInactiveRoles()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);

        var activeRole = new Role { Name = "ActiveRole", IsActive = true };
        var inactiveRole = new Role { Name = "InactiveRole", IsActive = false };

        context.Roles.AddRange(activeRole, inactiveRole);
        await context.SaveChangesAsync();

        var handler = new GetRoleLookupQueryHandler(repository);
        var query = new GetRoleLookupQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("ActiveRole", result[0].Name);
        Assert.DoesNotContain(result, r => r.Name == "InactiveRole");
    }

    [Fact]
    public async Task Handle_FiltersOutExcludeRoleId_WhenSpecified()
    {
        // Arrange
        using var context = CreateDbContext();
        var repository = new GenericRepository<Role>(context);

        var role1 = new Role { Name = "Role1", IsActive = true };
        var role2 = new Role { Name = "Role2", IsActive = true };
        var role3 = new Role { Name = "Role3", IsActive = true };

        context.Roles.AddRange(role1, role2, role3);
        await context.SaveChangesAsync();

        var handler = new GetRoleLookupQueryHandler(repository);
        var query = new GetRoleLookupQuery(ExcludeRoleId: role2.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, r => r.Id == role2.Id);
        Assert.Contains(result, r => r.Id == role1.Id);
        Assert.Contains(result, r => r.Id == role3.Id);
    }
}
