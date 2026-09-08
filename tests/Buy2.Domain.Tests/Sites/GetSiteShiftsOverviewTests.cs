using Buy2.Application.DTOs.Sites;
using Buy2.Application.Features.Sites.GetSiteShiftsOverview;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Sites;

public class GetSiteShiftsOverviewTests
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
    public async Task GetSiteShiftsOverview_NoShifts_ReturnsNoShiftsStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var region = new Region { Id = 1, Name = "Cairo" };
        var site = new Site
        {
            Id = 1,
            SiteName = "Site Cairo",
            Address = "Tahrir Square",
            RegionId = 1,
            Region = region
        };
        context.Regions.Add(region);
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var handler = new GetSiteShiftsOverviewQueryHandler(siteRepo);

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert
        Assert.Single(result);
        var overview = result[0];
        Assert.Equal(site.Id, overview.SiteId);
        Assert.Equal("Site Cairo", overview.SiteName);
        Assert.Equal("Cairo", overview.RegionName);
        Assert.Equal("Tahrir Square", overview.Address);
        Assert.Equal(3, overview.Days.Count);
        Assert.All(overview.Days, d =>
        {
            Assert.Equal(CoverageHealthStatus.NoShifts, d.Status);
            Assert.Equal(0, d.TotalShifts);
            Assert.Equal(0, d.OpenShifts);
            Assert.Equal(0, d.FilledShifts);
        });
    }

    [Fact]
    public async Task GetSiteShiftsOverview_FullCoverage_ReturnsFullStatus()
    {
        // Arrange: Day 1 has 2 shifts, both filled (open = 0)
        using var context = CreateDbContext();
        var today = DateTimeOffset.UtcNow.Date;
        var region = new Region { Id = 1, Name = "Alexandria" };
        var site = new Site
        {
            Id = 1,
            SiteName = "Site Alex",
            Address = "Corniche",
            RegionId = 1,
            Region = region
        };

        var shift1 = new ShiftEntity
        {
            Id = 1,
            SiteId = 1,
            StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(16), TimeSpan.Zero),
            EmployeeId = 101
        };
        var shift2 = new ShiftEntity
        {
            Id = 2,
            SiteId = 1,
            StartTime = new DateTimeOffset(today.AddHours(16), TimeSpan.Zero),
            EndTime = new DateTimeOffset(today.AddHours(23), TimeSpan.Zero),
            EmployeeId = 102
        };

        context.Regions.Add(region);
        context.Sites.Add(site);
        context.ShiftEntities.AddRange(shift1, shift2);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var handler = new GetSiteShiftsOverviewQueryHandler(siteRepo);

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert
        Assert.Single(result);
        var day1 = result[0].Days[0];
        Assert.Equal(DateOnly.FromDateTime(today), day1.Date);
        Assert.Equal(CoverageHealthStatus.Full, day1.Status);
        Assert.Equal(2, day1.TotalShifts);
        Assert.Equal(0, day1.OpenShifts);
        Assert.Equal(2, day1.FilledShifts);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_PartialCoverage_ReturnsPartialStatus()
    {
        // Arrange: Day 1 has 4 shifts, 3 filled and 1 open (75% filled >= 50%)
        using var context = CreateDbContext();
        var today = DateTimeOffset.UtcNow.Date;
        var region = new Region { Id = 1, Name = "Giza" };
        var site = new Site
        {
            Id = 1,
            SiteName = "Site Giza",
            Address = "Pyramids St",
            RegionId = 1,
            Region = region
        };

        var shifts = new List<ShiftEntity>
        {
            new() { Id = 1, SiteId = 1, StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero), EmployeeId = 101 },
            new() { Id = 2, SiteId = 1, StartTime = new DateTimeOffset(today.AddHours(10), TimeSpan.Zero), EmployeeId = 102 },
            new() { Id = 3, SiteId = 1, StartTime = new DateTimeOffset(today.AddHours(12), TimeSpan.Zero), EmployeeId = 103 },
            new() { Id = 4, SiteId = 1, StartTime = new DateTimeOffset(today.AddHours(14), TimeSpan.Zero), EmployeeId = null } // open
        };

        context.Regions.Add(region);
        context.Sites.Add(site);
        context.ShiftEntities.AddRange(shifts);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var handler = new GetSiteShiftsOverviewQueryHandler(siteRepo);

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert
        Assert.Single(result);
        var day1 = result[0].Days[0];
        Assert.Equal(CoverageHealthStatus.Partial, day1.Status);
        Assert.Equal(4, day1.TotalShifts);
        Assert.Equal(1, day1.OpenShifts);
        Assert.Equal(3, day1.FilledShifts);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_Exactly50PercentFilled_ReturnsPartialStatus()
    {
        // Arrange: 2 shifts, 1 filled and 1 open (50% filled >= 50%)
        using var context = CreateDbContext();
        var today = DateTimeOffset.UtcNow.Date;
        var region = new Region { Id = 1, Name = "Cairo" };
        var site = new Site { Id = 1, SiteName = "Site Half", Address = "Main Rd", RegionId = 1, Region = region };
        var shifts = new List<ShiftEntity>
        {
            new() { Id = 1, SiteId = 1, StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero), EmployeeId = 101 },
            new() { Id = 2, SiteId = 1, StartTime = new DateTimeOffset(today.AddHours(14), TimeSpan.Zero), EmployeeId = null }
        };

        context.Regions.Add(region);
        context.Sites.Add(site);
        context.ShiftEntities.AddRange(shifts);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var handler = new GetSiteShiftsOverviewQueryHandler(siteRepo);

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert
        Assert.Single(result);
        var day1 = result[0].Days[0];
        Assert.Equal(CoverageHealthStatus.Partial, day1.Status);
        Assert.Equal(2, day1.TotalShifts);
        Assert.Equal(1, day1.OpenShifts);
        Assert.Equal(1, day1.FilledShifts);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_CriticalCoverage_LessThan50PercentFilled_ReturnsCriticalStatus()
    {
        // Arrange: 4 shifts, 1 filled, 3 open (25% filled < 50%)
        using var context = CreateDbContext();
        var today = DateTimeOffset.UtcNow.Date;
        var region = new Region { Id = 1, Name = "Cairo" };
        var site = new Site { Id = 1, SiteName = "Site Critical", Address = "Ring Rd", RegionId = 1, Region = region };
        var shifts = new List<ShiftEntity>
        {
            new() { Id = 1, SiteId = 1, StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero), EmployeeId = 101 },
            new() { Id = 2, SiteId = 1, StartTime = new DateTimeOffset(today.AddHours(10), TimeSpan.Zero), EmployeeId = null },
            new() { Id = 3, SiteId = 1, StartTime = new DateTimeOffset(today.AddHours(12), TimeSpan.Zero), EmployeeId = null },
            new() { Id = 4, SiteId = 1, StartTime = new DateTimeOffset(today.AddHours(14), TimeSpan.Zero), EmployeeId = null }
        };

        context.Regions.Add(region);
        context.Sites.Add(site);
        context.ShiftEntities.AddRange(shifts);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var handler = new GetSiteShiftsOverviewQueryHandler(siteRepo);

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert
        Assert.Single(result);
        var day1 = result[0].Days[0];
        Assert.Equal(CoverageHealthStatus.Critical, day1.Status);
        Assert.Equal(4, day1.TotalShifts);
        Assert.Equal(3, day1.OpenShifts);
        Assert.Equal(1, day1.FilledShifts);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_AllShiftsOpen_ReturnsCriticalStatus()
    {
        // Arrange: 2 shifts, all open (0% filled)
        using var context = CreateDbContext();
        var today = DateTimeOffset.UtcNow.Date;
        var region = new Region { Id = 1, Name = "Cairo" };
        var site = new Site { Id = 1, SiteName = "Site All Open", Address = "Downtown", RegionId = 1, Region = region };
        var shifts = new List<ShiftEntity>
        {
            new() { Id = 1, SiteId = 1, StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero), EmployeeId = null },
            new() { Id = 2, SiteId = 1, StartTime = new DateTimeOffset(today.AddHours(14), TimeSpan.Zero), EmployeeId = null }
        };

        context.Regions.Add(region);
        context.Sites.Add(site);
        context.ShiftEntities.AddRange(shifts);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var handler = new GetSiteShiftsOverviewQueryHandler(siteRepo);

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert
        Assert.Single(result);
        var day1 = result[0].Days[0];
        Assert.Equal(CoverageHealthStatus.Critical, day1.Status);
        Assert.Equal(2, day1.TotalShifts);
        Assert.Equal(2, day1.OpenShifts);
        Assert.Equal(0, day1.FilledShifts);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_MultiDayCalculation_AssignsShiftsToCorrectDaysAndIgnoresOutliers()
    {
        // Arrange: Shifts across today, tomorrow, day 3, and day 4 (ignored)
        using var context = CreateDbContext();
        var today = DateTimeOffset.UtcNow.Date;
        var region = new Region { Id = 1, Name = "Cairo" };
        var site = new Site { Id = 1, SiteName = "Multi Day Site", Address = "Station Road", RegionId = 1, Region = region };

        var shifts = new List<ShiftEntity>
        {
            // Day 1 (today): 1 filled -> Full
            new() { Id = 1, SiteId = 1, StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero), EmployeeId = 1 },
            // Day 2 (today + 1): 2 shifts, 1 filled, 1 open -> Partial
            new() { Id = 2, SiteId = 1, StartTime = new DateTimeOffset(today.AddDays(1).AddHours(8), TimeSpan.Zero), EmployeeId = 2 },
            new() { Id = 3, SiteId = 1, StartTime = new DateTimeOffset(today.AddDays(1).AddHours(12), TimeSpan.Zero), EmployeeId = null },
            // Day 3 (today + 2): 1 open -> Critical
            new() { Id = 4, SiteId = 1, StartTime = new DateTimeOffset(today.AddDays(2).AddHours(8), TimeSpan.Zero), EmployeeId = null },
            // Outlier (today + 4 days): should be ignored
            new() { Id = 5, SiteId = 1, StartTime = new DateTimeOffset(today.AddDays(4).AddHours(8), TimeSpan.Zero), EmployeeId = 5 }
        };

        context.Regions.Add(region);
        context.Sites.Add(site);
        context.ShiftEntities.AddRange(shifts);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var handler = new GetSiteShiftsOverviewQueryHandler(siteRepo);

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(), CancellationToken.None);

        // Assert
        var overview = Assert.Single(result);
        Assert.Equal(3, overview.Days.Count);

        Assert.Equal(DateOnly.FromDateTime(today), overview.Days[0].Date);
        Assert.Equal(CoverageHealthStatus.Full, overview.Days[0].Status);

        Assert.Equal(DateOnly.FromDateTime(today.AddDays(1)), overview.Days[1].Date);
        Assert.Equal(CoverageHealthStatus.Partial, overview.Days[1].Status);

        Assert.Equal(DateOnly.FromDateTime(today.AddDays(2)), overview.Days[2].Date);
        Assert.Equal(CoverageHealthStatus.Critical, overview.Days[2].Status);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_RegionFilter_ReturnsOnlyMatchingRegionSites()
    {
        // Arrange
        using var context = CreateDbContext();
        var region1 = new Region { Id = 1, Name = "North" };
        var region2 = new Region { Id = 2, Name = "South" };

        var site1 = new Site { Id = 1, SiteName = "North Site", Address = "North St", RegionId = 1, Region = region1 };
        var site2 = new Site { Id = 2, SiteName = "South Site", Address = "South St", RegionId = 2, Region = region2 };

        context.Regions.AddRange(region1, region2);
        context.Sites.AddRange(site1, site2);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var handler = new GetSiteShiftsOverviewQueryHandler(siteRepo);

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(RegionId: 1), CancellationToken.None);

        // Assert
        var item = Assert.Single(result);
        Assert.Equal(1, item.SiteId);
        Assert.Equal("North Site", item.SiteName);
    }

    [Theory]
    [InlineData("downtown", 1)] // matches site1 by SiteName (case-insensitive)
    [InlineData("AVENUE", 2)]   // matches site2 by Address (case-insensitive)
    public async Task GetSiteShiftsOverview_SearchFilter_MatchesSiteNameAndAddressCaseInsensitively(string search, int expectedSiteId)
    {
        // Arrange
        using var context = CreateDbContext();
        var region = new Region { Id = 1, Name = "Main Region" };
        var site1 = new Site { Id = 1, SiteName = "Downtown Branch", Address = "100 Broadway", RegionId = 1, Region = region };
        var site2 = new Site { Id = 2, SiteName = "Airport Terminal", Address = "500 Grand Avenue", RegionId = 1, Region = region };

        context.Regions.Add(region);
        context.Sites.AddRange(site1, site2);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var handler = new GetSiteShiftsOverviewQueryHandler(siteRepo);

        // Act
        var result = await handler.Handle(new GetSiteShiftsOverviewQuery(Search: search), CancellationToken.None);

        // Assert
        var item = Assert.Single(result);
        Assert.Equal(expectedSiteId, item.SiteId);
    }

    [Fact]
    public async Task GetSiteShiftsOverview_CoverageStatusFilter_ReturnsOnlySitesMatchingStatusOnAnyDay()
    {
        // Arrange:
        // Site 1: Day 1 Full, Day 2 NoShifts, Day 3 NoShifts
        // Site 2: Day 1 Critical, Day 2 Critical, Day 3 Critical
        using var context = CreateDbContext();
        var today = DateTimeOffset.UtcNow.Date;
        var region = new Region { Id = 1, Name = "Main Region" };

        var site1 = new Site { Id = 1, SiteName = "Site Full", Address = "Addr 1", RegionId = 1, Region = region };
        var site2 = new Site { Id = 2, SiteName = "Site Crit", Address = "Addr 2", RegionId = 1, Region = region };

        // Site 1 has filled shift today
        var shift1 = new ShiftEntity { Id = 1, SiteId = 1, StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero), EmployeeId = 100 };
        // Site 2 has open shift today
        var shift2 = new ShiftEntity { Id = 2, SiteId = 2, StartTime = new DateTimeOffset(today.AddHours(8), TimeSpan.Zero), EmployeeId = null };

        context.Regions.Add(region);
        context.Sites.AddRange(site1, site2);
        context.ShiftEntities.AddRange(shift1, shift2);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var handler = new GetSiteShiftsOverviewQueryHandler(siteRepo);

        // Act 1: Filter by Full
        var fullResults = await handler.Handle(new GetSiteShiftsOverviewQuery(CoverageStatus: CoverageHealthStatus.Full), CancellationToken.None);
        // Act 2: Filter by Critical
        var critResults = await handler.Handle(new GetSiteShiftsOverviewQuery(CoverageStatus: CoverageHealthStatus.Critical), CancellationToken.None);

        // Assert
        Assert.Single(fullResults);
        Assert.Equal(1, fullResults[0].SiteId);

        Assert.Single(critResults);
        Assert.Equal(2, critResults[0].SiteId);
    }
}
