using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.GetSiteShiftsOverview;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Schedules;

public class GetSiteShiftsOverviewDashboardTests
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
    public async Task GetSiteShiftsOverview_Covered_AllShiftsAssignedToMatchingJobRoleEmployee_ReturnsCoveredStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var today = DateTimeOffset.UtcNow.Date;

        var region = new Region { Id = 1, Name = "Cairo Region" };
        var site = new Site
        {
            Id = 1,
            SiteName = "Downtown Branch",
            Address = "123 Nile St",
            RegionId = 1,
            Region = region
        };

        var employee1 = new Employee { Id = 10, JobRoleId = 1 };
        var employee2 = new Employee { Id = 11, JobRoleId = 2 };

        var shift1 = new ShiftEntity
        {
            Id = 1,
            SiteId = 1,
            JobRoleId = 1,
            EmployeeId = 10,
            StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(16), TimeSpan.Zero) // 8 hours
        };

        var shift2 = new ShiftEntity
        {
            Id = 2,
            SiteId = 1,
            JobRoleId = 2,
            EmployeeId = 11,
            StartTime = new DateTimeOffset(today.AddHours(16), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(23), TimeSpan.Zero) // 7 hours
        };

        context.Regions.Add(region);
        context.Sites.Add(site);
        context.Employees.AddRange(employee1, employee2);
        context.ShiftEntities.AddRange(shift1, shift2);
        await context.SaveChangesAsync();

        var handler = new GetSiteShiftsOverviewQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalCount);
        var card = Assert.Single(result.Items);
        Assert.Equal(1, card.SiteId);
        Assert.Equal("Downtown Branch", card.SiteName);
        Assert.Equal("123 Nile St", card.Address);
        Assert.Equal(1, card.RegionId);
        Assert.Equal("Cairo Region", card.RegionName);
        Assert.Equal(2, card.TotalShifts);
        Assert.Equal(0, card.OpenShifts);
        Assert.Equal(2, card.FilledShifts);
        Assert.Equal(ShiftCoverageHealthStatus.Covered, card.Status);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_Covered_ZeroShifts_ReturnsCoveredStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var region = new Region { Id = 1, Name = "Alex Region" };
        var site = new Site
        {
            Id = 1,
            SiteName = "Empty Site",
            Address = "Corniche",
            RegionId = 1,
            Region = region
        };

        context.Regions.Add(region);
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var handler = new GetSiteShiftsOverviewQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert
        var card = Assert.Single(result.Items);
        Assert.Equal(0, card.TotalShifts);
        Assert.Equal(0, card.OpenShifts);
        Assert.Equal(0, card.FilledShifts);
        Assert.Equal(ShiftCoverageHealthStatus.Covered, card.Status);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_Shortage_AtLeastOneShiftUnassigned_ReturnsShortageStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var today = DateTimeOffset.UtcNow.Date;

        var region = new Region { Id = 1, Name = "Cairo" };
        var site = new Site { Id = 1, SiteName = "Site Cairo", Address = "Tahrir", RegionId = 1, Region = region };
        var employee = new Employee { Id = 10, JobRoleId = 1 };

        var shift1 = new ShiftEntity
        {
            Id = 1,
            SiteId = 1,
            JobRoleId = 1,
            EmployeeId = 10,
            StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(16), TimeSpan.Zero)
        };

        var shift2 = new ShiftEntity
        {
            Id = 2,
            SiteId = 1,
            JobRoleId = 1,
            EmployeeId = null, // Unassigned
            StartTime = new DateTimeOffset(today.AddHours(16), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(23), TimeSpan.Zero)
        };

        context.Regions.Add(region);
        context.Sites.Add(site);
        context.Employees.Add(employee);
        context.ShiftEntities.AddRange(shift1, shift2);
        await context.SaveChangesAsync();

        var handler = new GetSiteShiftsOverviewQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert
        var card = Assert.Single(result.Items);
        Assert.Equal(2, card.TotalShifts);
        Assert.Equal(1, card.OpenShifts);
        Assert.Equal(1, card.FilledShifts);
        Assert.Equal(ShiftCoverageHealthStatus.Shortage, card.Status);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_OvertimeRisk_ShiftDurationGreaterThan8Hours_ReturnsOvertimeRiskStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var today = DateTimeOffset.UtcNow.Date;

        var region = new Region { Id = 1, Name = "Cairo" };
        var site = new Site { Id = 1, SiteName = "Site Cairo", Address = "Tahrir", RegionId = 1, Region = region };
        var employee = new Employee { Id = 10, JobRoleId = 1 };

        var shift = new ShiftEntity
        {
            Id = 1,
            SiteId = 1,
            JobRoleId = 1,
            EmployeeId = 10,
            StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(17), TimeSpan.Zero) // 9 hours (> 8 hours)
        };

        context.Regions.Add(region);
        context.Sites.Add(site);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = new GetSiteShiftsOverviewQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert
        var card = Assert.Single(result.Items);
        Assert.Equal(ShiftCoverageHealthStatus.OvertimeRisk, card.Status);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_OvertimeRisk_EmployeeAssignedMultipleShiftsInSameDay_ReturnsOvertimeRiskStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var today = DateTimeOffset.UtcNow.Date;

        var region = new Region { Id = 1, Name = "Cairo" };
        var site = new Site { Id = 1, SiteName = "Site Cairo", Address = "Tahrir", RegionId = 1, Region = region };
        var employee = new Employee { Id = 10, JobRoleId = 1 };

        var shift1 = new ShiftEntity
        {
            Id = 1,
            SiteId = 1,
            JobRoleId = 1,
            EmployeeId = 10,
            StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(12), TimeSpan.Zero) // 4 hours
        };

        var shift2 = new ShiftEntity
        {
            Id = 2,
            SiteId = 1,
            JobRoleId = 1,
            EmployeeId = 10, // Same employee on same day
            StartTime = new DateTimeOffset(today.AddHours(14), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(18), TimeSpan.Zero) // 4 hours
        };

        context.Regions.Add(region);
        context.Sites.Add(site);
        context.Employees.Add(employee);
        context.ShiftEntities.AddRange(shift1, shift2);
        await context.SaveChangesAsync();

        var handler = new GetSiteShiftsOverviewQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert
        var card = Assert.Single(result.Items);
        Assert.Equal(ShiftCoverageHealthStatus.OvertimeRisk, card.Status);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_UnqualifiedAssignment_EmployeeHasDifferentJobRoleId_ReturnsUnqualifiedAssignmentStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var today = DateTimeOffset.UtcNow.Date;

        var region = new Region { Id = 1, Name = "Cairo" };
        var site = new Site { Id = 1, SiteName = "Site Cairo", Address = "Tahrir", RegionId = 1, Region = region };
        var employee = new Employee { Id = 10, JobRoleId = 1 }; // Employee has JobRoleId 1

        var shift = new ShiftEntity
        {
            Id = 1,
            SiteId = 1,
            JobRoleId = 2, // Shift requires JobRoleId 2
            EmployeeId = 10,
            StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(16), TimeSpan.Zero)
        };

        context.Regions.Add(region);
        context.Sites.Add(site);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = new GetSiteShiftsOverviewQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert
        var card = Assert.Single(result.Items);
        Assert.Equal(ShiftCoverageHealthStatus.UnqualifiedAssignment, card.Status);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_Precedence_UnqualifiedAssignmentOverridesShortageAndOvertimeRisk()
    {
        // Arrange: Site has unqualified shift, overtime shift, and open shift
        using var context = CreateDbContext();
        var today = DateTimeOffset.UtcNow.Date;

        var region = new Region { Id = 1, Name = "Cairo" };
        var site = new Site { Id = 1, SiteName = "Site Cairo", Address = "Tahrir", RegionId = 1, Region = region };
        var unqualifiedEmp = new Employee { Id = 10, JobRoleId = 1 };
        var qualifiedEmp = new Employee { Id = 11, JobRoleId = 2 };

        var shiftUnqualified = new ShiftEntity
        {
            Id = 1,
            SiteId = 1,
            JobRoleId = 2, // Mismatch (employee is 1)
            EmployeeId = 10,
            StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(16), TimeSpan.Zero)
        };

        var shiftOvertime = new ShiftEntity
        {
            Id = 2,
            SiteId = 1,
            JobRoleId = 2,
            EmployeeId = 11,
            StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(18), TimeSpan.Zero) // 10 hours
        };

        var shiftShortage = new ShiftEntity
        {
            Id = 3,
            SiteId = 1,
            JobRoleId = 2,
            EmployeeId = null, // Shortage
            StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(16), TimeSpan.Zero)
        };

        context.Regions.Add(region);
        context.Sites.Add(site);
        context.Employees.AddRange(unqualifiedEmp, qualifiedEmp);
        context.ShiftEntities.AddRange(shiftUnqualified, shiftOvertime, shiftShortage);
        await context.SaveChangesAsync();

        var handler = new GetSiteShiftsOverviewQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert: UnqualifiedAssignment > OvertimeRisk > Shortage
        var card = Assert.Single(result.Items);
        Assert.Equal(ShiftCoverageHealthStatus.UnqualifiedAssignment, card.Status);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_Precedence_OvertimeRiskOverridesShortage()
    {
        // Arrange: Site has overtime shift and open shift (shortage)
        using var context = CreateDbContext();
        var today = DateTimeOffset.UtcNow.Date;

        var region = new Region { Id = 1, Name = "Cairo" };
        var site = new Site { Id = 1, SiteName = "Site Cairo", Address = "Tahrir", RegionId = 1, Region = region };
        var employee = new Employee { Id = 10, JobRoleId = 1 };

        var shiftOvertime = new ShiftEntity
        {
            Id = 1,
            SiteId = 1,
            JobRoleId = 1,
            EmployeeId = 10,
            StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(18), TimeSpan.Zero) // 10 hours
        };

        var shiftShortage = new ShiftEntity
        {
            Id = 2,
            SiteId = 1,
            JobRoleId = 1,
            EmployeeId = null,
            StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(16), TimeSpan.Zero)
        };

        context.Regions.Add(region);
        context.Sites.Add(site);
        context.Employees.Add(employee);
        context.ShiftEntities.AddRange(shiftOvertime, shiftShortage);
        await context.SaveChangesAsync();

        var handler = new GetSiteShiftsOverviewQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert: OvertimeRisk > Shortage
        var card = Assert.Single(result.Items);
        Assert.Equal(ShiftCoverageHealthStatus.OvertimeRisk, card.Status);
    }

    [Theory]
    [InlineData("headquarters", 1)] // matches site1 by SiteName (case-insensitive)
    [InlineData("PYRAMIDS", 2)]     // matches site2 by Address (case-insensitive)
    public async Task GetSiteShiftsOverview_SearchFilter_MatchesSiteNameAndAddress(string search, int expectedSiteId)
    {
        // Arrange
        using var context = CreateDbContext();
        var region = new Region { Id = 1, Name = "Main Region" };
        var site1 = new Site { Id = 1, SiteName = "Cairo Headquarters", Address = "Tahrir Square", RegionId = 1, Region = region };
        var site2 = new Site { Id = 2, SiteName = "Giza Hub", Address = "Pyramids Road", RegionId = 1, Region = region };

        context.Regions.Add(region);
        context.Sites.AddRange(site1, site2);
        await context.SaveChangesAsync();

        var handler = new GetSiteShiftsOverviewQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(Search: search), CancellationToken.None);

        // Assert
        var card = Assert.Single(result.Items);
        Assert.Equal(expectedSiteId, card.SiteId);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_RegionFilter_ReturnsOnlyMatchingRegionSites()
    {
        // Arrange
        using var context = CreateDbContext();
        var region1 = new Region { Id = 1, Name = "North" };
        var region2 = new Region { Id = 2, Name = "South" };

        var site1 = new Site { Id = 1, SiteName = "North Branch", Address = "North St", RegionId = 1, Region = region1 };
        var site2 = new Site { Id = 2, SiteName = "South Branch", Address = "South St", RegionId = 2, Region = region2 };

        context.Regions.AddRange(region1, region2);
        context.Sites.AddRange(site1, site2);
        await context.SaveChangesAsync();

        var handler = new GetSiteShiftsOverviewQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(RegionId: 1), CancellationToken.None);

        // Assert
        var card = Assert.Single(result.Items);
        Assert.Equal(1, card.SiteId);
        Assert.Equal("North Branch", card.SiteName);
        Assert.Equal("North", card.RegionName);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_Pagination_ReturnsCorrectPagePageSizeTotalCountAndSlicedItems()
    {
        // Arrange: 5 sites (Ids 1..5)
        using var context = CreateDbContext();
        var region = new Region { Id = 1, Name = "Region" };
        context.Regions.Add(region);

        for (int i = 1; i <= 5; i++)
        {
            context.Sites.Add(new Site
            {
                Id = i,
                SiteName = $"Site {i}",
                Address = $"Address {i}",
                RegionId = 1,
                Region = region
            });
        }
        await context.SaveChangesAsync();

        var handler = new GetSiteShiftsOverviewQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act: Page 2, PageSize 2 -> should return Sites 3 and 4
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(Page: 2, PageSize: 2), CancellationToken.None);

        // Assert
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(3, result.Items[0].SiteId);
        Assert.Equal(4, result.Items[1].SiteId);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_IgnoresShiftsOutsideNext3Days()
    {
        // Arrange
        using var context = CreateDbContext();
        var today = DateTimeOffset.UtcNow.Date;

        var region = new Region { Id = 1, Name = "Cairo" };
        var site = new Site { Id = 1, SiteName = "Site Cairo", Address = "Tahrir", RegionId = 1, Region = region };
        var employee = new Employee { Id = 10, JobRoleId = 1 };

        // In window (today)
        var shiftInWindow = new ShiftEntity
        {
            Id = 1,
            SiteId = 1,
            JobRoleId = 1,
            EmployeeId = 10,
            StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(16), TimeSpan.Zero)
        };

        // Outside window (4 days from now) - unassigned
        var shiftOutsideWindow = new ShiftEntity
        {
            Id = 2,
            SiteId = 1,
            JobRoleId = 1,
            EmployeeId = null, // would trigger Shortage if included
            StartTime = new DateTimeOffset(today.AddDays(4).AddHours(8), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddDays(4).AddHours(16), TimeSpan.Zero)
        };

        context.Regions.Add(region);
        context.Sites.Add(site);
        context.Employees.Add(employee);
        context.ShiftEntities.AddRange(shiftInWindow, shiftOutsideWindow);
        await context.SaveChangesAsync();

        var handler = new GetSiteShiftsOverviewQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert: Outside window shift ignored -> status remains Covered and TotalShifts = 1
        var card = Assert.Single(result.Items);
        Assert.Equal(1, card.TotalShifts);
        Assert.Equal(0, card.OpenShifts);
        Assert.Equal(1, card.FilledShifts);
        Assert.Equal(ShiftCoverageHealthStatus.Covered, card.Status);
    }
}
