using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Application.Features.ShiftTemplates.GetShiftTemplates;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.ShiftTemplates;

public class GetShiftTemplatesSortingTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Buy2DbContext(options);
    }

    private static ShiftTemplate CreateTemplate(
        string name,
        DateTime createdAt,
        DateTimeOffset? updatedAt = null,
        int assignedSites = 0)
    {
        var template = new ShiftTemplate
        {
            Name = name,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };

        for (var i = 0; i < assignedSites; i++)
        {
            template.ShiftTemplateSites.Add(new ShiftTemplateSite { SiteId = 1000 + i });
        }

        return template;
    }

    private async Task SeedAsync(Buy2DbContext context)
    {
        context.ShiftTemplates.AddRange(
            CreateTemplate("Bravo", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), assignedSites: 1),
            CreateTemplate("Alpha", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero), assignedSites: 3),
            CreateTemplate("Charlie", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero), assignedSites: 2));
        await context.SaveChangesAsync();
    }

    private static string[] NamesOf(ShiftTemplatePaginatedResponseDto<ShiftTemplateListItemDto> response)
    {
        return response.Items.Select(i => i.Name).ToArray();
    }

    [Fact]
    public async Task DefaultsToCreationDesc_WhenNoSortSent()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = new GetShiftTemplatesQueryHandler(new GenericRepository<ShiftTemplate>(context));

        var result = await handler.Handle(
            new GetShiftTemplatesQuery(new ShiftTemplateFilterQueryDto()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Charlie", "Alpha", "Bravo"], NamesOf(result.Value!));
    }

    [Fact]
    public async Task SortsByNameAsc_WhenOnlyNameSortSent()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = new GetShiftTemplatesQueryHandler(new GenericRepository<ShiftTemplate>(context));

        var result = await handler.Handle(
            new GetShiftTemplatesQuery(new ShiftTemplateFilterQueryDto(NameSort: "asc")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Alpha", "Bravo", "Charlie"], NamesOf(result.Value!));
    }

    [Fact]
    public async Task SortsByCreationAsc_WhenOnlyCreationSortSent()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = new GetShiftTemplatesQueryHandler(new GenericRepository<ShiftTemplate>(context));

        var result = await handler.Handle(
            new GetShiftTemplatesQuery(new ShiftTemplateFilterQueryDto(CreationSort: "asc")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Bravo", "Alpha", "Charlie"], NamesOf(result.Value!));
    }

    [Fact]
    public async Task SortsByUpdatedAsc_WhenOnlyUpdatedSortSent()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = new GetShiftTemplatesQueryHandler(new GenericRepository<ShiftTemplate>(context));

        var result = await handler.Handle(
            new GetShiftTemplatesQuery(new ShiftTemplateFilterQueryDto(UpdatedSort: "asc")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Alpha", "Charlie", "Bravo"], NamesOf(result.Value!));
    }

    [Fact]
    public async Task SortsByAssignedAsc_WhenOnlyNumberOfAssignedSortSent()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = new GetShiftTemplatesQueryHandler(new GenericRepository<ShiftTemplate>(context));

        var result = await handler.Handle(
            new GetShiftTemplatesQuery(new ShiftTemplateFilterQueryDto(NumberOfAssignedSort: "asc")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Bravo", "Charlie", "Alpha"], NamesOf(result.Value!));
    }

    [Fact]
    public async Task RejectsInvalidSortDirection()
    {
        using var context = CreateDbContext();
        await SeedAsync(context);
        var handler = new GetShiftTemplatesQueryHandler(new GenericRepository<ShiftTemplate>(context));

        var result = await handler.Handle(
            new GetShiftTemplatesQuery(new ShiftTemplateFilterQueryDto(NameSort: "sideways")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
