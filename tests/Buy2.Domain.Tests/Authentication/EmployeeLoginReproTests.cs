using System.Security.Cryptography;
using System.Text;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs;
using Buy2.Application.Features.Authentication.Login;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Authentication;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Buy2.Domain.Tests.Authentication;

public class EmployeeLoginReproTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    [Fact]
    public async Task SeededEmployee_Employee2_ShouldHaveValidPasswordHash_ForDefaultPassword()
    {
        // Arrange: Seed database using system DatabaseSeeder
        using var context = CreateDbContext();
        await DatabaseSeeder.SeedAsync(context);

        // Act: Fetch seeded employee_2@buy2hrms.com
        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Email == "employee_2@buy2hrms.com");

        // Assert: Employee 2 should exist and have a valid SHA-256 hash of "Welcome@123" instead of literal "string"
        Assert.NotNull(employee);
        
        var expectedHash = HashPassword("Welcome@123");
        Assert.Equal(expectedHash, employee.PasswordHash);
    }
}
