using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Application.Features.ShiftTemplates.ExportShiftTemplates;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.ShiftTemplates;

public class ExportShiftTemplatesTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static void SeedPrincipals(Buy2DbContext context, int siteCount, int blockCount, int employeeBase)
    {
        if (context.JobRoles.Local.All(r => r.Id != 7))
        {
            context.JobRoles.Add(new JobRole { Id = 7, Title = "Crew" });
        }

        for (var i = 0; i < siteCount; i++)
        {
            var siteId = 500 + i;
            if (context.Sites.Local.All(s => s.Id != siteId))
            {
                context.Sites.Add(new Site { Id = siteId, SiteName = $"Site {siteId}" });
            }
        }

        for (var i = 0; i < blockCount; i++)
        {
            var employeeId = employeeBase + i;
            if (context.Employees.Local.All(e => e.Id != employeeId))
            {
                context.Employees.Add(new Employee
                {
                    Id = employeeId,
                    FirstName = $"Emp{employeeId}",
                    LastName = "Test",
                    EmployeeCode = $"EMP-{employeeId}",
                    JobRoleId = 7,
                    IsActive = true
                });
            }
        }
    }

    private static void SeedTemplate(
        Buy2DbContext context,
        int id,
        string name,
        DateTime createdAt,
        int siteCount,
        int blockCount,
        int employeeBase = 1000)
    {
        SeedPrincipals(context, siteCount, blockCount, employeeBase);
        context.ShiftTemplates.Add(new ShiftTemplate
        {
            Id = id,
            Name = name,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            CreatedAt = createdAt,
        });

        for (var i = 0; i < siteCount; i++)
        {
            context.ShiftTemplateSites.Add(new ShiftTemplateSite
            {
                ShiftTemplateId = id,
                SiteId = 500 + i
            });
        }

        for (var i = 0; i < blockCount; i++)
        {
            context.ShiftBlocks.Add(new ShiftBlock
            {
                ShiftTemplateId = id,
                StartTime = new TimeSpan(9 + i, 0, 0),
                EndTime = new TimeSpan(10 + i, 0, 0),
                JobRoleId = 7,
                EmployeeId = employeeBase + i
            });
        }
    }

    private static XLWorkbook OpenWorkbook(byte[] bytes)
    {
        return new XLWorkbook(new MemoryStream(bytes));
    }

    private static ExportShiftTemplatesQueryHandler CreateHandler(Buy2DbContext context)
    {
        return new ExportShiftTemplatesQueryHandler(new GenericRepository<ShiftTemplate>(context));
    }

    [Fact]
    public async Task Export_ReturnsThreeSheetsWithHeadersAndData()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Morning", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            siteCount: 2, blockCount: 1);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(
            new ExportShiftTemplatesQuery(new ShiftTemplateFilterQueryDto()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        using var workbook = OpenWorkbook(result.Value!);

        Assert.True(workbook.Worksheets.Contains("Templates"));
        Assert.True(workbook.Worksheets.Contains("Blocks"));
        Assert.True(workbook.Worksheets.Contains("Sites"));

        var templates = workbook.Worksheet("Templates");
        Assert.Equal("Id", templates.Cell(1, 1).GetString());
        Assert.Equal("Name", templates.Cell(1, 2).GetString());
        Assert.Equal("Sites", templates.Cell(1, 8).GetString());
        Assert.Equal("Morning", templates.Cell(2, 2).GetString());
        Assert.Equal(2, templates.Cell(2, 7).GetValue<int>());
        Assert.Contains("Site 500", templates.Cell(2, 8).GetString());
        Assert.True(templates.Row(1).Style.Font.Bold);

        var blocks = workbook.Worksheet("Blocks");
        Assert.Equal("TemplateId", blocks.Cell(1, 1).GetString());
        Assert.Equal("AssignedUserName", blocks.Cell(1, 7).GetString());
        Assert.Equal("Crew", blocks.Cell(2, 6).GetString());
        Assert.Contains("Emp1000", blocks.Cell(2, 7).GetString());

        var sites = workbook.Worksheet("Sites");
        Assert.Equal("SiteName", sites.Cell(1, 4).GetString());
        Assert.Equal("Site 500", sites.Cell(2, 4).GetString());
        Assert.Equal("Site 501", sites.Cell(3, 4).GetString());
    }

    [Fact]
    public async Task Export_RespectsSearchTermFilter()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Alpha", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            siteCount: 1, blockCount: 1, employeeBase: 1000);
        SeedTemplate(context, 2, "Beta", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            siteCount: 1, blockCount: 1, employeeBase: 2000);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(
            new ExportShiftTemplatesQuery(new ShiftTemplateFilterQueryDto(SearchTerm: "Alpha")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        using var workbook = OpenWorkbook(result.Value!);
        var templates = workbook.Worksheet("Templates");

        Assert.Equal("Alpha", templates.Cell(2, 2).GetString());
        Assert.True(templates.Cell(3, 2).IsEmpty());
    }

    [Fact]
    public async Task Export_IgnoresPaginationParams()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Alpha", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            siteCount: 0, blockCount: 0);
        SeedTemplate(context, 2, "Beta", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            siteCount: 0, blockCount: 0);
        SeedTemplate(context, 3, "Gamma", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            siteCount: 0, blockCount: 0);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(
            new ExportShiftTemplatesQuery(new ShiftTemplateFilterQueryDto(PageNumber: 2, PageSize: 1)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        using var workbook = OpenWorkbook(result.Value!);
        var templates = workbook.Worksheet("Templates");

        Assert.False(templates.Cell(2, 1).IsEmpty());
        Assert.False(templates.Cell(3, 1).IsEmpty());
        Assert.False(templates.Cell(4, 1).IsEmpty());
        Assert.True(templates.Cell(5, 1).IsEmpty());
    }

    [Fact]
    public async Task Export_RespectsNameSorting()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Charlie", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            siteCount: 0, blockCount: 0);
        SeedTemplate(context, 2, "Alpha", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            siteCount: 0, blockCount: 0);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(
            new ExportShiftTemplatesQuery(new ShiftTemplateFilterQueryDto(NameSort: "asc")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        using var workbook = OpenWorkbook(result.Value!);
        var templates = workbook.Worksheet("Templates");

        Assert.Equal("Alpha", templates.Cell(2, 2).GetString());
        Assert.Equal("Charlie", templates.Cell(3, 2).GetString());
    }

    [Fact]
    public async Task Export_EmptyDatabase_ReturnsHeadersOnly()
    {
        using var context = CreateDbContext();

        var result = await CreateHandler(context).Handle(
            new ExportShiftTemplatesQuery(new ShiftTemplateFilterQueryDto()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        using var workbook = OpenWorkbook(result.Value!);

        Assert.Equal("Name", workbook.Worksheet("Templates").Cell(1, 2).GetString());
        Assert.True(workbook.Worksheet("Templates").Cell(2, 1).IsEmpty());
        Assert.True(workbook.Worksheet("Blocks").Cell(2, 1).IsEmpty());
        Assert.True(workbook.Worksheet("Sites").Cell(2, 1).IsEmpty());
    }

    [Fact]
    public async Task Export_RejectsInvalidSortDirection()
    {
        using var context = CreateDbContext();

        var result = await CreateHandler(context).Handle(
            new ExportShiftTemplatesQuery(new ShiftTemplateFilterQueryDto(NameSort: "sideways")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
