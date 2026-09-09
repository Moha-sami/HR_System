using System.Text.Json;
using Buy2.Api.Controllers;
using Buy2.Application.Common.Models;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Application.Features.ShiftTemplates.DuplicateShiftTemplate;
using Buy2.Application.Features.ShiftTemplates.GetShiftTemplateById;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.ShiftTemplates;

public class DuplicateShiftTemplateTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static DuplicateShiftTemplateCommandHandler CreateHandler(Buy2DbContext context)
    {
        return new DuplicateShiftTemplateCommandHandler(
            new GenericRepository<ShiftTemplate>(context),
            new UnitOfWork(context));
    }

    private static void SeedPrincipals(Buy2DbContext context, int siteCount, int blockCount, int employeeBase)
    {
        // InMemory drops Included children whose principals are missing, while
        // production FKs (Restrict) guarantee they exist. Seed principals so the
        // tests mirror production referential integrity.
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

    private static ShiftTemplate SeedTemplate(
        Buy2DbContext context,
        int id,
        string name,
        int siteCount,
        int blockCount,
        int employeeBase = 1000)
    {
        SeedPrincipals(context, siteCount, blockCount, employeeBase);
        var template = new ShiftTemplate
        {
            Id = id,
            Name = name,
            StartTime = new TimeSpan(22, 0, 0),
            EndTime = new TimeSpan(6, 0, 0),
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)
        };
        context.ShiftTemplates.Add(template);

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
                StartTime = TimeSpan.FromHours(i % 24),
                EndTime = TimeSpan.FromHours((i % 24) + 1),
                JobRoleId = 7,
                EmployeeId = employeeBase + i
            });
        }

        return template;
    }

    // TC01 + TC02 + TC03 + TC-RESP-01/02/03: duplicate valid template.
    [Fact]
    public async Task Duplicate_ValidTemplate_ReturnsNewIdAndCopy1Name()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Morning_Shift", siteCount: 2, blockCount: 3);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(1, result.Value.Id);
        Assert.Equal("Morning_Shift_copy1", result.Value.Name);
        Assert.True(result.Value.Id > 0);
    }

    // TC-RESP-04: contract is Id + Name only.
    [Fact]
    public async Task Duplicate_ResponseContract_ContainsIdAndNameOnly()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Morning_Shift", siteCount: 1, blockCount: 1);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dtoProperties = typeof(DuplicateShiftTemplateResponseDto)
            .GetProperties()
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToArray();
        Assert.Equal(["Id", "Name"], dtoProperties);

        var json = JsonSerializer.Serialize(
            result.Value,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var keys = JsonDocument.Parse(json).RootElement.EnumerateObject()
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToArray();
        Assert.Equal(["id", "name"], keys);
    }

    // TC-RESP-05: returned ID resolves via GET to the full details of the copy.
    [Fact]
    public async Task Duplicate_CreatedResource_GetByIdReturnsFullDetails()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Night_Shift", siteCount: 3, blockCount: 5);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var duplicate = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);

        Assert.True(duplicate.IsSuccess);
        var getHandler = new GetShiftTemplateByIdQueryHandler(new GenericRepository<ShiftTemplate>(context));
        var details = await getHandler.Handle(
            new GetShiftTemplateByIdQuery(duplicate.Value!.Id),
            CancellationToken.None);

        Assert.True(details.IsSuccess);
        Assert.Equal("Night_Shift_copy1", details.Value!.Name);
        Assert.Equal(3, details.Value.NumberOfAssignedSites);
        Assert.Equal(5, details.Value.ShiftBlocks.Count);
    }

    // TC04 + TC-NAME-03: copy1 exists -> copy2.
    [Fact]
    public async Task Duplicate_WhenCopy1Exists_CreatesCopy2()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Morning_Shift", siteCount: 1, blockCount: 1);
        SeedTemplate(context, 2, "Morning_Shift_copy1", siteCount: 0, blockCount: 0);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Morning_Shift_copy2", result.Value!.Name);
    }

    // TC05: copy1..copy3 exist -> copy4.
    [Fact]
    public async Task Duplicate_WhenMultipleCopiesExist_CreatesNextIndex()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Morning_Shift", siteCount: 0, blockCount: 0);
        SeedTemplate(context, 2, "Morning_Shift_copy1", siteCount: 0, blockCount: 0);
        SeedTemplate(context, 3, "Morning_Shift_copy2", siteCount: 0, blockCount: 0);
        SeedTemplate(context, 4, "Morning_Shift_copy3", siteCount: 0, blockCount: 0);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Morning_Shift_copy4", result.Value!.Name);
    }

    // Gap fills: copy1 + copy3 exist -> copy2.
    [Fact]
    public async Task Duplicate_WhenIndexGapExists_FillsSmallestAvailableIndex()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Morning_Shift", siteCount: 0, blockCount: 0);
        SeedTemplate(context, 2, "Morning_Shift_copy1", siteCount: 0, blockCount: 0);
        SeedTemplate(context, 3, "Morning_Shift_copy3", siteCount: 0, blockCount: 0);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Morning_Shift_copy2", result.Value!.Name);
    }

    // TC06: timing bounds are cloned exactly.
    [Fact]
    public async Task Duplicate_CopiesTimingBoundsExactly()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Night_Shift", siteCount: 1, blockCount: 2);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var copy = await context.ShiftTemplates.AsNoTracking()
            .FirstAsync(t => t.Id == result.Value!.Id);
        Assert.Equal(new TimeSpan(22, 0, 0), copy.StartTime);
        Assert.Equal(new TimeSpan(6, 0, 0), copy.EndTime);
    }

    // TC07: all site links are cloned.
    [Fact]
    public async Task Duplicate_CopiesAllSiteLinks()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Night_Shift", siteCount: 3, blockCount: 1);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var sourceSites = await context.ShiftTemplateSites.AsNoTracking()
            .Where(s => s.ShiftTemplateId == 1).Select(s => s.SiteId).OrderBy(id => id).ToListAsync();
        var copySites = await context.ShiftTemplateSites.AsNoTracking()
            .Where(s => s.ShiftTemplateId == result.Value!.Id).Select(s => s.SiteId).OrderBy(id => id).ToListAsync();
        Assert.Equal(sourceSites, copySites);
        Assert.Equal(3, copySites.Count);
    }

    // TC08: all child blocks cloned with same times/roles/order, unassigned.
    [Fact]
    public async Task Duplicate_CopiesAllChildBlocksUnassigned()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Night_Shift", siteCount: 1, blockCount: 5);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var sourceBlocks = await context.ShiftBlocks.AsNoTracking()
            .Where(b => b.ShiftTemplateId == 1).OrderBy(b => b.Id).ToListAsync();
        var copyBlocks = await context.ShiftBlocks.AsNoTracking()
            .Where(b => b.ShiftTemplateId == result.Value!.Id).OrderBy(b => b.Id).ToListAsync();

        Assert.Equal(sourceBlocks.Count, copyBlocks.Count);
        Assert.Equal(5, copyBlocks.Count);
        for (var i = 0; i < sourceBlocks.Count; i++)
        {
            Assert.Equal(sourceBlocks[i].StartTime, copyBlocks[i].StartTime);
            Assert.Equal(sourceBlocks[i].EndTime, copyBlocks[i].EndTime);
            Assert.Equal(sourceBlocks[i].JobRoleId, copyBlocks[i].JobRoleId);
            Assert.Null(copyBlocks[i].EmployeeId);
            Assert.NotEqual(sourceBlocks[i].Id, copyBlocks[i].Id);
        }
    }

    // TC09: editing the duplicate does not affect the source.
    [Fact]
    public async Task Duplicate_ModifyingCopy_DoesNotAffectSource()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Night_Shift", siteCount: 2, blockCount: 2);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);
        Assert.True(result.IsSuccess);

        var copyBlock = await context.ShiftBlocks
            .FirstAsync(b => b.ShiftTemplateId == result.Value!.Id);
        copyBlock.StartTime = new TimeSpan(11, 33, 0);
        var copyLink = await context.ShiftTemplateSites
            .FirstAsync(s => s.ShiftTemplateId == result.Value!.Id);
        context.ShiftTemplateSites.Remove(copyLink);
        await context.SaveChangesAsync();

        var sourceBlocks = await context.ShiftBlocks.AsNoTracking()
            .Where(b => b.ShiftTemplateId == 1).ToListAsync();
        var sourceLinks = await context.ShiftTemplateSites.AsNoTracking()
            .Where(s => s.ShiftTemplateId == 1).ToListAsync();
        Assert.Equal(2, sourceBlocks.Count);
        Assert.All(sourceBlocks, b => Assert.NotEqual(new TimeSpan(11, 33, 0), b.StartTime));
        Assert.Equal(2, sourceLinks.Count);
    }

    // TC10: source snapshot identical before/after.
    [Fact]
    public async Task Duplicate_SourceRemainsUnchanged()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Night_Shift", siteCount: 3, blockCount: 5);
        await context.SaveChangesAsync();

        var before = await context.ShiftTemplates.AsNoTracking()
            .Include(t => t.ShiftTemplateSites)
            .Include(t => t.ShiftBlocks)
            .FirstAsync(t => t.Id == 1);
        var beforeSites = before.ShiftTemplateSites.Select(s => s.SiteId).OrderBy(id => id).ToArray();
        var beforeBlocks = before.ShiftBlocks
            .OrderBy(b => b.Id)
            .Select(b => (b.StartTime, b.EndTime, b.JobRoleId, b.EmployeeId))
            .ToArray();

        var handler = CreateHandler(context);
        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);
        Assert.True(result.IsSuccess);

        var after = await context.ShiftTemplates.AsNoTracking()
            .Include(t => t.ShiftTemplateSites)
            .Include(t => t.ShiftBlocks)
            .FirstAsync(t => t.Id == 1);
        Assert.Equal(before.Name, after.Name);
        Assert.Equal(before.StartTime, after.StartTime);
        Assert.Equal(before.EndTime, after.EndTime);
        Assert.Equal(beforeSites, after.ShiftTemplateSites.Select(s => s.SiteId).OrderBy(id => id).ToArray());
        Assert.Equal(beforeBlocks, after.ShiftBlocks
            .OrderBy(b => b.Id)
            .Select(b => (b.StartTime, b.EndTime, b.JobRoleId, b.EmployeeId))
            .ToArray());
    }

    // TC11 + TC12: createdAt/updatedAt are set to now within tolerance.
    [Fact]
    public async Task Duplicate_SetsCreationAndUpdateTimestampsToNow()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Night_Shift", siteCount: 0, blockCount: 0);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var before = DateTime.UtcNow;
        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);
        var after = DateTime.UtcNow;

        Assert.True(result.IsSuccess);
        var copy = await context.ShiftTemplates.AsNoTracking()
            .FirstAsync(t => t.Id == result.Value!.Id);
        Assert.InRange(copy.CreatedAt, before.AddSeconds(-5), after.AddSeconds(5));
        Assert.NotNull(copy.UpdatedAt);
        Assert.InRange(copy.UpdatedAt!.Value.UtcDateTime, before.AddSeconds(-5), after.AddSeconds(5));
    }

    // TC14: no child blocks -> success with none.
    [Fact]
    public async Task Duplicate_TemplateWithNoBlocks_SucceedsWithNoBlocks()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Solo", siteCount: 2, blockCount: 0);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(await context.ShiftBlocks.AsNoTracking()
            .Where(b => b.ShiftTemplateId == result.Value!.Id).ToListAsync());
    }

    // TC15: no site links -> success with none.
    [Fact]
    public async Task Duplicate_TemplateWithNoSites_SucceedsWithNoSites()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Solo", siteCount: 0, blockCount: 2);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(await context.ShiftTemplateSites.AsNoTracking()
            .Where(s => s.ShiftTemplateId == result.Value!.Id).ToListAsync());
    }

    // TC16: large child data deep-cloned without loss.
    [Fact]
    public async Task Duplicate_LargeTemplate_ClonesEverything()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Mega", siteCount: 10, blockCount: 30);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, await context.ShiftTemplateSites.AsNoTracking()
            .CountAsync(s => s.ShiftTemplateId == result.Value!.Id));
        var copyBlocks = await context.ShiftBlocks.AsNoTracking()
            .Where(b => b.ShiftTemplateId == result.Value!.Id).ToListAsync();
        Assert.Equal(30, copyBlocks.Count);
        Assert.All(copyBlocks, b => Assert.Null(b.EmployeeId));
    }

    // TC17: unknown ID -> NotFound, nothing created.
    [Fact]
    public async Task Duplicate_UnknownId_ReturnsNotFoundAndCreatesNothing()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Night_Shift", siteCount: 1, blockCount: 1);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(9999, ActorEmployeeId: null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
        Assert.Equal(1, await context.ShiftTemplates.CountAsync());
    }

    // TC-NAME-01: 100-char source -> exactly 100 chars ending with _copy1.
    [Fact]
    public void BuildCandidateName_Source100Chars_EndsWithCopy1At100Chars()
    {
        var source = new string('A', 100);

        var candidate = DuplicateShiftTemplateCommandHandler.BuildCandidateName(source, 1);

        Assert.Equal(100, candidate.Length);
        Assert.EndsWith("_copy1", candidate);
    }

    // TC-NAME-02: 100-char source, index 10 -> fits _copy10 within 100.
    [Theory]
    [InlineData(1, 100)]
    [InlineData(9, 100)]
    [InlineData(10, 100)]
    [InlineData(99, 100)]
    [InlineData(100, 99)]
    [InlineData(999, 100)]
    public void BuildCandidateName_LongSource_PreservesFullSuffixWithinLimit(int index, int sourceLength)
    {
        var source = new string('B', sourceLength);

        var candidate = DuplicateShiftTemplateCommandHandler.BuildCandidateName(source, index);

        Assert.True(candidate.Length <= 100);
        Assert.EndsWith($"_copy{index}", candidate);
    }

    // TC-NAME-04 + TC-NAME-05: case-insensitive copy search.
    [Fact]
    public async Task Duplicate_ExistingCopyDifferentCase_SelectsNextIndex()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "Source", siteCount: 0, blockCount: 0);
        SeedTemplate(context, 2, "source_COPY1", siteCount: 0, blockCount: 0);
        SeedTemplate(context, 3, "SOURCE_copy2", siteCount: 0, blockCount: 0);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Source_copy3", result.Value!.Name);
    }

    // TC-NAME-06: very long source + high index stays within limit.
    [Fact]
    public async Task Duplicate_LongSourceHighIndex_StaysWithinLimit()
    {
        using var context = CreateDbContext();
        var longName = new string('C', 100);
        SeedTemplate(context, 1, longName, siteCount: 1, blockCount: 1);
        for (var i = 1; i <= 9; i++)
        {
            SeedTemplate(context, 100 + i, DuplicateShiftTemplateCommandHandler.BuildCandidateName(longName, i), siteCount: 0, blockCount: 0);
        }

        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Name.Length <= 100);
        Assert.EndsWith("_copy10", result.Value.Name);
    }

    // TC19: no duplicate name collision.
    [Fact]
    public async Task Duplicate_NeverReusesExistingName()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 1, "X", siteCount: 0, blockCount: 0);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var first = await handler.Handle(new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null), CancellationToken.None);
        var second = await handler.Handle(new DuplicateShiftTemplateCommand(1, ActorEmployeeId: null), CancellationToken.None);

        Assert.True(first.IsSuccess && second.IsSuccess);
        Assert.Equal("X_copy1", first.Value!.Name);
        Assert.Equal("X_copy2", second.Value!.Name);
        Assert.NotEqual(first.Value.Id, second.Value.Id);
    }

    // TC20: single action carries no site/block payload and clones everything.
    [Fact]
    public void Duplicate_CommandCarriesNoChildPayload()
    {
        var parameters = typeof(DuplicateShiftTemplateCommand)
            .GetProperties()
            .Where(p => p.Name != "EqualityContract")
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToArray();

        Assert.Equal(["ActorEmployeeId", "Id"], parameters);
    }

    // E2E: Night_Shift with timing + 3 sites + 5 blocks.
    [Fact]
    public async Task Duplicate_NightShiftEndToEnd_ClonesEverything()
    {
        using var context = CreateDbContext();
        SeedTemplate(context, 10, "Night_Shift", siteCount: 3, blockCount: 5);
        await context.SaveChangesAsync();
        var before = DateTime.UtcNow;
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new DuplicateShiftTemplateCommand(10, ActorEmployeeId: 42),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(10, result.Value!.Id);
        Assert.Equal("Night_Shift_copy1", result.Value.Name);

        var copy = await context.ShiftTemplates.AsNoTracking()
            .Include(t => t.ShiftTemplateSites)
            .Include(t => t.ShiftBlocks)
            .FirstAsync(t => t.Id == result.Value.Id);
        Assert.Equal(new TimeSpan(22, 0, 0), copy.StartTime);
        Assert.Equal(new TimeSpan(6, 0, 0), copy.EndTime);
        Assert.Equal(
            new[] { 500, 501, 502 },
            copy.ShiftTemplateSites.Select(s => s.SiteId).OrderBy(id => id).ToArray());
        Assert.Equal(5, copy.ShiftBlocks.Count);
        Assert.All(copy.ShiftBlocks, b => Assert.Null(b.EmployeeId));
        Assert.InRange(copy.CreatedAt, before.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
        Assert.NotNull(copy.UpdatedAt);
        Assert.Equal(42, copy.LastUpdatedByEmployeeId);

        var source = await context.ShiftTemplates.AsNoTracking()
            .Include(t => t.ShiftTemplateSites)
            .Include(t => t.ShiftBlocks)
            .FirstAsync(t => t.Id == 10);
        Assert.Equal("Night_Shift", source.Name);
        Assert.Equal(3, source.ShiftTemplateSites.Count);
        Assert.Equal(5, source.ShiftBlocks.Count);
        Assert.All(source.ShiftBlocks, b => Assert.NotNull(b.EmployeeId));
    }

    // TC18 + TC-RESP-01: controller maps invalid format -> 400 without calling mediator.
    [Fact]
    public async Task Controller_InvalidIdFormat_ReturnsBadRequest()
    {
        var sender = new CannedSender(null);
        var controller = new ShiftTemplatesController(sender)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.DuplicateShiftTemplate("abc", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.False(sender.SendCalled);
    }

    // TC-RESP-01: controller maps success -> 201 Created.
    [Fact]
    public async Task Controller_ValidDuplicate_Returns201Created()
    {
        var dto = new DuplicateShiftTemplateResponseDto(9, "Night_Shift_copy1");
        var sender = new CannedSender(Result<DuplicateShiftTemplateResponseDto>.Success(dto));
        var controller = new ShiftTemplatesController(sender)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.DuplicateShiftTemplate("5", CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(dto, created.Value);
    }

    // TC17 at API level: unknown ID -> 404.
    [Fact]
    public async Task Controller_UnknownId_ReturnsNotFound()
    {
        var sender = new CannedSender(
            Result<DuplicateShiftTemplateResponseDto>.NotFound("Shift template with ID 9999 was not found."));
        var controller = new ShiftTemplatesController(sender)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.DuplicateShiftTemplate("9999", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    private sealed class CannedSender : ISender
    {
        private readonly object? _response;

        public CannedSender(object? response)
        {
            _response = response;
        }

        public bool SendCalled { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SendCalled = true;
            return Task.FromResult((TResponse)_response!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            throw new NotSupportedException();
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            return Task.CompletedTask;
        }
    }
}
