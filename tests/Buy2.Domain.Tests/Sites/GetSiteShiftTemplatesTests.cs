using Buy2.Api.Controllers;
using Buy2.Application.Features.Sites.GetSiteShiftTemplates;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Sites;

public class GetSiteShiftTemplatesTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static async Task SeedAsync(Buy2DbContext context)
    {
        context.Sites.Add(new Site { Id = 1, SiteName = "Cairo HQ" });
        context.Sites.Add(new Site { Id = 2, SiteName = "Giza Branch" });

        var morning = new ShiftTemplate
        {
            Id = 1,
            Name = "Morning Shift",
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(16, 0, 0)
        };
        var night = new ShiftTemplate
        {
            Id = 2,
            Name = "Night Shift",
            StartTime = new TimeSpan(22, 0, 0),
            EndTime = new TimeSpan(6, 0, 0)
        };
        var empty = new ShiftTemplate
        {
            Id = 3,
            Name = "Empty Template",
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0)
        };
        var otherSite = new ShiftTemplate
        {
            Id = 4,
            Name = "Other Site Shift",
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0)
        };
        context.ShiftTemplates.AddRange(morning, night, empty, otherSite);

        context.ShiftTemplateSites.AddRange(
            new ShiftTemplateSite { Id = 1, ShiftTemplateId = 1, SiteId = 1 },
            new ShiftTemplateSite { Id = 2, ShiftTemplateId = 2, SiteId = 1 },
            // Template 2 is linked to a second site: must still appear exactly once for site 1.
            new ShiftTemplateSite { Id = 3, ShiftTemplateId = 2, SiteId = 2 },
            new ShiftTemplateSite { Id = 4, ShiftTemplateId = 3, SiteId = 1 },
            new ShiftTemplateSite { Id = 5, ShiftTemplateId = 4, SiteId = 2 });

        context.ShiftBlocks.AddRange(
            new ShiftBlock { Id = 1, ShiftTemplateId = 1, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(12, 0, 0), JobRoleId = 1, EmployeeId = 1 },
            new ShiftBlock { Id = 2, ShiftTemplateId = 2, StartTime = new TimeSpan(22, 0, 0), EndTime = new TimeSpan(2, 0, 0), JobRoleId = 1, EmployeeId = 2 },
            new ShiftBlock { Id = 3, ShiftTemplateId = 2, StartTime = new TimeSpan(2, 0, 0), EndTime = new TimeSpan(6, 0, 0), JobRoleId = 1, EmployeeId = 3 });

        await context.SaveChangesAsync();
    }

    private static GetSiteShiftTemplatesQueryHandler CreateHandler(Buy2DbContext context)
    {
        return new GetSiteShiftTemplatesQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftTemplate>(context));
    }

    [Fact]
    public async Task NonExistentSite_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetSiteShiftTemplatesQuery(999, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task ValidSiteWithNoTemplates_ReturnsEmptyList()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 10, SiteName = "Lonely Site" });
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetSiteShiftTemplatesQuery(10, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task ValidSite_ReturnsOnlyLinkedTemplates()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetSiteShiftTemplatesQuery(1, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.Count);
        Assert.DoesNotContain(result.Value, t => t.Name == "Other Site Shift");
    }

    [Fact]
    public async Task TemplateLinkedToMultipleSites_AppearsOnceWithCorrectBlockCount()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetSiteShiftTemplatesQuery(1, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var night = Assert.Single(result.Value!, t => t.Name == "Night Shift");
        Assert.Equal(2, night.TotalBlockCount);
        Assert.Equal(2, night.ShiftBlocks.Count);
    }

    [Fact]
    public async Task ZeroBlockTemplate_IncludedWithCountZero()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetSiteShiftTemplatesQuery(1, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var empty = Assert.Single(result.Value!, t => t.Name == "Empty Template");
        Assert.Equal(0, empty.TotalBlockCount);
        Assert.Empty(empty.ShiftBlocks);
    }

    [Fact]
    public async Task OvernightShift_IncludedWithCorrectTimes()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetSiteShiftTemplatesQuery(1, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var night = Assert.Single(result.Value!, t => t.Name == "Night Shift");
        Assert.Equal("10:00 PM", night.StartTime);
        Assert.Equal("06:00 AM", night.EndTime);
    }

    [Fact]
    public async Task SearchExactMatch_ReturnsMatchingTemplates()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetSiteShiftTemplatesQuery(1, "Night Shift"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Night Shift", result.Value![0].Name);
    }

    [Fact]
    public async Task SearchPartialMatch_ReturnsMatchingTemplates()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetSiteShiftTemplatesQuery(1, "Shift"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task SearchCaseInsensitive_ReturnsSameResults()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = CreateHandler(context);

        var lower = await handler.Handle(new GetSiteShiftTemplatesQuery(1, "night"), CancellationToken.None);
        var mixed = await handler.Handle(new GetSiteShiftTemplatesQuery(1, "Night"), CancellationToken.None);
        var upper = await handler.Handle(new GetSiteShiftTemplatesQuery(1, "NIGHT"), CancellationToken.None);

        Assert.True(lower.IsSuccess && mixed.IsSuccess && upper.IsSuccess);
        Assert.Equal(mixed.Value!.Select(t => t.Id), lower.Value!.Select(t => t.Id));
        Assert.Equal(mixed.Value!.Select(t => t.Id), upper.Value!.Select(t => t.Id));
        Assert.Single(lower.Value!);
    }

    [Fact]
    public async Task SearchNoMatch_ReturnsEmptyList()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetSiteShiftTemplatesQuery(1, "NoSuchTemplate"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task EmptyAndWhitespaceSearch_ReturnsAllTemplates()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = CreateHandler(context);

        var nullSearch = await handler.Handle(new GetSiteShiftTemplatesQuery(1, null), CancellationToken.None);
        var emptySearch = await handler.Handle(new GetSiteShiftTemplatesQuery(1, ""), CancellationToken.None);
        var whitespaceSearch = await handler.Handle(new GetSiteShiftTemplatesQuery(1, "   "), CancellationToken.None);

        Assert.Equal(3, nullSearch.Value!.Count);
        Assert.Equal(3, emptySearch.Value!.Count);
        Assert.Equal(3, whitespaceSearch.Value!.Count);
    }

    [Fact]
    public async Task SearchWithLeadingTrailingSpaces_IsTrimmed()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetSiteShiftTemplatesQuery(1, "   Night   "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Night Shift", result.Value![0].Name);
    }

    [Fact]
    public void Action_HasUnconstrainedRouteAndRoleBasedAuth()
    {
        var method = typeof(GetSitesController).GetMethod(nameof(GetSitesController.GetSiteShiftTemplates));
        Assert.NotNull(method);

        // No :int constraint so malformed ids (e.g. "abc") fail model binding -> 400, not 404.
        var httpGet = Assert.Single(method.GetCustomAttributes(typeof(HttpGetAttribute), false));
        Assert.Equal("{siteId}/shift-templates", ((HttpGetAttribute)httpGet).Template);

        var authorize = Assert.Single(method.GetCustomAttributes(typeof(AuthorizeAttribute), false));
        var roles = ((AuthorizeAttribute)authorize).Roles?.Split(',').Select(r => r.Trim()).ToList() ?? [];
        Assert.Contains("Admin", roles);
        Assert.Contains("Manager", roles);
    }
}
