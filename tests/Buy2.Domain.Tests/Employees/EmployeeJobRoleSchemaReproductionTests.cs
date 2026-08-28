using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Buy2.Domain.Tests.Employees;

public class EmployeeJobRoleSchemaReproductionTests
{
    [Fact]
    public void EmployeeAndJobRole_ModelShouldBeSynchronizedWithMigrations_WithoutPendingChanges()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseSqlServer("Server=localhost;Database=Dummy;Trusted_Connection=True;Encrypt=False;")
            .Options;

        using var context = new Buy2DbContext(options);

        // This should return false (no pending changes).
        // It currently returns true (failing) because JobRole has IsDeleted/DeletedAt without a migration.
        var hasPendingModelChanges = context.Database.HasPendingModelChanges();

        Assert.False(hasPendingModelChanges, "The EF Core model has pending model changes (e.g., JobRole.IsDeleted/DeletedAt) not captured in migrations.");
    }
}
