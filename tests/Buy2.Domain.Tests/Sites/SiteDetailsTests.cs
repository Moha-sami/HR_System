using Buy2.Application.Features.Sites.GetSiteDetails;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Sites;

public class SiteDetailsTests
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
    public async Task GetSiteBasicInfo_NonExistentSite_ThrowsKeyNotFoundException()
    {
        using var context = CreateDbContext();
        var siteRepo = new GenericRepository<Site>(context);
        var handler = new GetSiteBasicInfoQueryHandler(siteRepo);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new GetSiteBasicInfoQuery(999), CancellationToken.None));
    }

    [Fact]
    public async Task GetSiteBasicInfo_ValidSite_ReturnsFullProfile()
    {
        using var context = CreateDbContext();
        var region = new Region { Id = 1, Name = "Cairo Region" };
        var site = new Site
        {
            Id = 1,
            SiteName = "Cairo HQ",
            RegionId = 1,
            Region = region,
            Address = "123 Main St",
            PhoneNumber = "+201000000000",
            MacAddress = "00:1A:2B:3C:4D:5E",
            MapUrl = "https://maps.google.com/?q=cairo",
            Instructions = "Enter through gate 1"
        };
        context.Regions.Add(region);
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var handler = new GetSiteBasicInfoQueryHandler(siteRepo);

        var result = await handler.Handle(new GetSiteBasicInfoQuery(1), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Cairo HQ", result.SiteName);
        Assert.Equal("Cairo Region", result.RegionName);
        Assert.Equal("123 Main St", result.Address);
        Assert.Equal("+201000000000", result.PhoneNumber);
        Assert.Equal("00:1A:2B:3C:4D:5E", result.MacAddress);
    }

    [Fact]
    public async Task GetSiteShifts_NonExistentSite_ThrowsKeyNotFoundException()
    {
        using var context = CreateDbContext();
        var siteRepo = new GenericRepository<Site>(context);
        var shiftRepo = new GenericRepository<ShiftEntity>(context);
        var handler = new GetSiteShiftsQueryHandler(siteRepo, shiftRepo);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new GetSiteShiftsQuery(999), CancellationToken.None));
    }

    [Fact]
    public async Task GetSiteShifts_ValidSite_ReturnsShiftList()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Cairo HQ" };
        var template = new ShiftTemplate { Id = 1, Name = "Morning Shift", RequiredHeadcount = 5 };
        var role = new JobRole { Id = 1, Title = "Security Officer", DepartmentId = 1 };
        var shift = new ShiftEntity
        {
            Id = 1,
            SiteId = 1,
            ShiftTemplateId = 1,
            ShiftTemplate = template,
            JobRoleId = 1,
            JobRole = role,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(8),
            IsPublished = true
        };
        context.Sites.Add(site);
        context.ShiftTemplates.Add(template);
        context.JobRoles.Add(role);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var shiftRepo = new GenericRepository<ShiftEntity>(context);
        var handler = new GetSiteShiftsQueryHandler(siteRepo, shiftRepo);

        var result = await handler.Handle(new GetSiteShiftsQuery(1), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Morning Shift", result[0].ShiftName);
        Assert.True(result[0].IsSmartPostingEnabled);
        Assert.Single(result[0].Roles);
        Assert.Equal("Security Officer", result[0].Roles[0].RoleName);
        Assert.Equal(5, result[0].Roles[0].RequiredHeadcount);
    }

    [Fact]
    public async Task GetSiteEmployees_NonExistentSite_ThrowsKeyNotFoundException()
    {
        using var context = CreateDbContext();
        var siteRepo = new GenericRepository<Site>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var handler = new GetSiteEmployeesQueryHandler(siteRepo, empRepo);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new GetSiteEmployeesQuery(999), CancellationToken.None));
    }

    [Fact]
    public async Task GetSiteEmployees_ValidSite_ReturnsAssignedEmployeesWithCorrectStatus()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Cairo HQ" };
        var role = new JobRole { Id = 1, Title = "HR Specialist", DepartmentId = 1 };
        var emp1 = new Employee
        {
            Id = 1,
            FirstName = "Ahmed",
            LastName = "Ali",
            Email = "ahmed@example.com",
            PhoneNumber = "+201111111111",
            SiteId = 1,
            JobRole = role,
            IsActive = true,
            IsDeleted = false
        };
        var emp2 = new Employee
        {
            Id = 2,
            FirstName = "Sara",
            LastName = "Hassan",
            Email = "sara@example.com",
            PhoneNumber = "+201222222222",
            SiteId = 1,
            JobRole = role,
            IsActive = false,
            IsDeleted = false
        };
        var emp3 = new Employee
        {
            Id = 3,
            FirstName = "Omar",
            LastName = "Khaled",
            Email = "omar@example.com",
            PhoneNumber = "+201333333333",
            SiteId = 1,
            JobRole = role,
            IsActive = false,
            IsDeleted = true
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.AddRange(emp1, emp2, emp3);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var handler = new GetSiteEmployeesQueryHandler(siteRepo, empRepo);

        var result = await handler.Handle(new GetSiteEmployeesQuery(1), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Active", result.First(e => e.EmployeeId == 1).Status);
        Assert.Equal("Suspended", result.First(e => e.EmployeeId == 2).Status);
        Assert.Equal("Terminated", result.First(e => e.EmployeeId == 3).Status);
    }
}
