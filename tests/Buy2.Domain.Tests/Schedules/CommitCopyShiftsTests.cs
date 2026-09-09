using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.CommitCopyShifts;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Schedules;

public class CommitCopyShiftsTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task Handle_SiteNotFound_ThrowsKeyNotFoundException()
    {
        using var context = CreateDbContext();
        var handler = new CommitCopyShiftsCommandHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new UnitOfWork(context)
        );

        var command = new CommitCopyShiftsCommand(
            SiteId: 999,
            SourceDate: new DateOnly(2026, 9, 1),
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 2) }
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TargetDatesEmpty_ThrowsArgumentException()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Main Branch" });
        await context.SaveChangesAsync();

        var handler = new CommitCopyShiftsCommandHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new UnitOfWork(context)
        );

        var commandNull = new CommitCopyShiftsCommand(
            SiteId: 1,
            SourceDate: new DateOnly(2026, 9, 1),
            TargetDates: null,
            DateResolutions: null
        );

        var commandEmpty = new CommitCopyShiftsCommand(
            SiteId: 1,
            SourceDate: new DateOnly(2026, 9, 1),
            TargetDates: new List<DateOnly>(),
            DateResolutions: new Dictionary<DateOnly, CopyConflictResolution>()
        );

        var commandSourceDateOnly = new CommitCopyShiftsCommand(
            SiteId: 1,
            SourceDate: new DateOnly(2026, 9, 1),
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 1) }
        );

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(commandNull, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(commandEmpty, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(commandSourceDateOnly, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SourceDateHasNoShifts_ThrowsInvalidOperationException()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Main Branch" });
        await context.SaveChangesAsync();

        var handler = new CommitCopyShiftsCommandHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new UnitOfWork(context)
        );

        var command = new CommitCopyShiftsCommand(
            SiteId: 1,
            SourceDate: new DateOnly(2026, 9, 1),
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 2) }
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("No shifts found on source date to copy.", ex.Message);
    }

    [Fact]
    public async Task Handle_ConflictFreeTargetDate_ShiftsClonedCleanly()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 10, Title = "Cashier" };
        var sourceDate = new DateOnly(2026, 9, 1);
        var targetDate = new DateOnly(2026, 9, 2);

        var start1 = new DateTimeOffset(sourceDate.ToDateTime(new TimeOnly(8, 0)), TimeSpan.FromHours(3));
        var start2 = new DateTimeOffset(sourceDate.ToDateTime(new TimeOnly(16, 0)), TimeSpan.FromHours(3));

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.ShiftEntities.AddRange(
            new ShiftEntity { Id = 1, SiteId = 1, JobRoleId = 10, EmployeeId = 5, StartTime = start1, EndTime = start1.AddHours(8), IsPublished = true },
            new ShiftEntity { Id = 2, SiteId = 1, JobRoleId = 10, EmployeeId = 6, StartTime = start2, EndTime = start2.AddHours(8), IsPublished = false }
        );
        await context.SaveChangesAsync();

        var handler = new CommitCopyShiftsCommandHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new UnitOfWork(context)
        );

        var command = new CommitCopyShiftsCommand(
            SiteId: 1,
            SourceDate: sourceDate,
            TargetDates: new List<DateOnly> { targetDate }
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.TotalDatesProcessed);
        Assert.Equal(1, result.CopiedDatesCount);
        Assert.Equal(0, result.SkippedDatesCount);
        Assert.Equal(2, result.TotalShiftsCreated);
        Assert.Equal(0, result.TotalShiftsReplaced);
        Assert.Contains(targetDate, result.CopiedDates);
        Assert.Empty(result.SkippedDates);

        var targetShifts = await context.ShiftEntities
            .Where(s => s.SiteId == 1 && s.StartTime >= new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) && s.StartTime < new DateTimeOffset(targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero))
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        Assert.Equal(2, targetShifts.Count);
        Assert.Equal(new TimeOnly(8, 0), TimeOnly.FromTimeSpan(targetShifts[0].StartTime.TimeOfDay));
        Assert.Equal(new TimeOnly(16, 0), TimeOnly.FromTimeSpan(targetShifts[1].StartTime.TimeOfDay));
        Assert.Equal(5, targetShifts[0].EmployeeId);
        Assert.Equal(6, targetShifts[1].EmployeeId);
        Assert.True(targetShifts[0].IsPublished);
        Assert.False(targetShifts[1].IsPublished);
    }

    [Fact]
    public async Task Handle_ConflictingTargetDate_WithReplace_DeletesExistingAndClonesShifts()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 10, Title = "Cashier" };
        var sourceDate = new DateOnly(2026, 9, 1);
        var targetDate = new DateOnly(2026, 9, 2);

        var sourceStart = new DateTimeOffset(sourceDate.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
        var existingStart = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.ShiftEntities.AddRange(
            new ShiftEntity { Id = 1, SiteId = 1, JobRoleId = 10, StartTime = sourceStart, EndTime = sourceStart.AddHours(8) },
            new ShiftEntity { Id = 99, SiteId = 1, JobRoleId = 10, StartTime = existingStart, EndTime = existingStart.AddHours(4) }
        );
        await context.SaveChangesAsync();

        var handler = new CommitCopyShiftsCommandHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new UnitOfWork(context)
        );

        var command = new CommitCopyShiftsCommand(
            SiteId: 1,
            SourceDate: sourceDate,
            DateResolutions: new Dictionary<DateOnly, CopyConflictResolution>
            {
                { targetDate, CopyConflictResolution.Replace }
            }
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.TotalDatesProcessed);
        Assert.Equal(1, result.CopiedDatesCount);
        Assert.Equal(0, result.SkippedDatesCount);
        Assert.Equal(1, result.TotalShiftsCreated);
        Assert.Equal(1, result.TotalShiftsReplaced);
        Assert.Contains(targetDate, result.CopiedDates);

        var oldShift = await context.ShiftEntities.FindAsync(99);
        Assert.Null(oldShift);

        var targetShifts = await context.ShiftEntities
            .Where(s => s.SiteId == 1 && s.StartTime >= new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) && s.StartTime < new DateTimeOffset(targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero))
            .ToListAsync();

        Assert.Single(targetShifts);
        Assert.NotEqual(99, targetShifts[0].Id);
        Assert.Equal(new TimeOnly(9, 0), TimeOnly.FromTimeSpan(targetShifts[0].StartTime.TimeOfDay));
    }

    [Fact]
    public async Task Handle_ConflictingTargetDate_WithKeepExisting_PreservesExistingAndSkipsCloning()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 10, Title = "Cashier" };
        var sourceDate = new DateOnly(2026, 9, 1);
        var targetDate = new DateOnly(2026, 9, 2);

        var sourceStart = new DateTimeOffset(sourceDate.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
        var existingStart = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.ShiftEntities.AddRange(
            new ShiftEntity { Id = 1, SiteId = 1, JobRoleId = 10, StartTime = sourceStart, EndTime = sourceStart.AddHours(8) },
            new ShiftEntity { Id = 99, SiteId = 1, JobRoleId = 10, StartTime = existingStart, EndTime = existingStart.AddHours(4) }
        );
        await context.SaveChangesAsync();

        var handler = new CommitCopyShiftsCommandHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new UnitOfWork(context)
        );

        var command = new CommitCopyShiftsCommand(
            SiteId: 1,
            SourceDate: sourceDate,
            DateResolutions: new Dictionary<DateOnly, CopyConflictResolution>
            {
                { targetDate, CopyConflictResolution.KeepExisting }
            }
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.TotalDatesProcessed);
        Assert.Equal(0, result.CopiedDatesCount);
        Assert.Equal(1, result.SkippedDatesCount);
        Assert.Equal(0, result.TotalShiftsCreated);
        Assert.Equal(0, result.TotalShiftsReplaced);
        Assert.Empty(result.CopiedDates);
        Assert.Contains(targetDate, result.SkippedDates);

        var oldShift = await context.ShiftEntities.FindAsync(99);
        Assert.NotNull(oldShift);
    }

    [Fact]
    public async Task Handle_BulkReplaceAll_OverwritesMultipleConflictingDatesWithoutIndividualEntries()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 10, Title = "Cashier" };
        var sourceDate = new DateOnly(2026, 9, 1);
        var target1 = new DateOnly(2026, 9, 2);
        var target2 = new DateOnly(2026, 9, 3);

        var sourceStart = new DateTimeOffset(sourceDate.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
        var exist1Start = new DateTimeOffset(target1.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);
        var exist2Start = new DateTimeOffset(target2.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero);

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.ShiftEntities.AddRange(
            new ShiftEntity { Id = 1, SiteId = 1, JobRoleId = 10, StartTime = sourceStart, EndTime = sourceStart.AddHours(8) },
            new ShiftEntity { Id = 88, SiteId = 1, JobRoleId = 10, StartTime = exist1Start, EndTime = exist1Start.AddHours(4) },
            new ShiftEntity { Id = 99, SiteId = 1, JobRoleId = 10, StartTime = exist2Start, EndTime = exist2Start.AddHours(4) }
        );
        await context.SaveChangesAsync();

        var handler = new CommitCopyShiftsCommandHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new UnitOfWork(context)
        );

        var command = new CommitCopyShiftsCommand(
            SiteId: 1,
            SourceDate: sourceDate,
            TargetDates: new List<DateOnly> { target1, target2 },
            BulkReplaceAll: true
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.TotalDatesProcessed);
        Assert.Equal(2, result.CopiedDatesCount);
        Assert.Equal(0, result.SkippedDatesCount);
        Assert.Equal(2, result.TotalShiftsCreated);
        Assert.Equal(2, result.TotalShiftsReplaced);
        Assert.Contains(target1, result.CopiedDates);
        Assert.Contains(target2, result.CopiedDates);

        Assert.Null(await context.ShiftEntities.FindAsync(88));
        Assert.Null(await context.ShiftEntities.FindAsync(99));
    }

    [Fact]
    public async Task Handle_MultiDate_MixedDecisions_Clean_Replace_KeepExisting()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 10, Title = "Cashier" };
        var sourceDate = new DateOnly(2026, 9, 1);
        var cleanDate = new DateOnly(2026, 9, 2);
        var replaceDate = new DateOnly(2026, 9, 3);
        var keepDate = new DateOnly(2026, 9, 4);

        var sourceStart1 = new DateTimeOffset(sourceDate.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
        var sourceStart2 = new DateTimeOffset(sourceDate.ToDateTime(new TimeOnly(16, 0)), TimeSpan.Zero);
        var replaceExisting = new DateTimeOffset(replaceDate.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);
        var keepExisting1 = new DateTimeOffset(keepDate.ToDateTime(new TimeOnly(11, 0)), TimeSpan.Zero);
        var keepExisting2 = new DateTimeOffset(keepDate.ToDateTime(new TimeOnly(15, 0)), TimeSpan.Zero);

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.ShiftEntities.AddRange(
            new ShiftEntity { Id = 1, SiteId = 1, JobRoleId = 10, StartTime = sourceStart1, EndTime = sourceStart1.AddHours(8) },
            new ShiftEntity { Id = 2, SiteId = 1, JobRoleId = 10, StartTime = sourceStart2, EndTime = sourceStart2.AddHours(8) },
            new ShiftEntity { Id = 50, SiteId = 1, JobRoleId = 10, StartTime = replaceExisting, EndTime = replaceExisting.AddHours(4) },
            new ShiftEntity { Id = 60, SiteId = 1, JobRoleId = 10, StartTime = keepExisting1, EndTime = keepExisting1.AddHours(4) },
            new ShiftEntity { Id = 61, SiteId = 1, JobRoleId = 10, StartTime = keepExisting2, EndTime = keepExisting2.AddHours(4) }
        );
        await context.SaveChangesAsync();

        var handler = new CommitCopyShiftsCommandHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new UnitOfWork(context)
        );

        var command = new CommitCopyShiftsCommand(
            SiteId: 1,
            SourceDate: sourceDate,
            TargetDates: new List<DateOnly> { cleanDate },
            DateResolutions: new Dictionary<DateOnly, CopyConflictResolution>
            {
                { replaceDate, CopyConflictResolution.Replace },
                { keepDate, CopyConflictResolution.KeepExisting }
            }
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(3, result.TotalDatesProcessed);
        Assert.Equal(2, result.CopiedDatesCount);
        Assert.Equal(1, result.SkippedDatesCount);
        Assert.Equal(4, result.TotalShiftsCreated);
        Assert.Equal(1, result.TotalShiftsReplaced);
        Assert.Equal(new List<DateOnly> { cleanDate, replaceDate }, result.CopiedDates);
        Assert.Equal(new List<DateOnly> { keepDate }, result.SkippedDates);

        Assert.Null(await context.ShiftEntities.FindAsync(50));
        Assert.NotNull(await context.ShiftEntities.FindAsync(60));
        Assert.NotNull(await context.ShiftEntities.FindAsync(61));
    }

    [Fact]
    public async Task Handle_PreservesShiftTimingRolesAndAssignments_AndRespectsCopyAssignmentsFalse()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 10, Title = "Barista" };
        var template = new ShiftTemplate { Id = 3, Name = "Morning Roast" };
        var sourceDate = new DateOnly(2026, 9, 1);
        var targetWithAssign = new DateOnly(2026, 9, 5);
        var targetWithoutAssign = new DateOnly(2026, 9, 6);

        var sourceStart = new DateTimeOffset(sourceDate.ToDateTime(new TimeOnly(7, 30)), TimeSpan.FromHours(2));
        var sourceEnd = sourceStart.AddHours(8).AddMinutes(30);

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.ShiftTemplates.Add(template);
        context.ShiftEntities.Add(new ShiftEntity
        {
            Id = 1,
            SiteId = 1,
            JobRoleId = 10,
            ShiftTemplateId = 3,
            EmployeeId = 42,
            StartTime = sourceStart,
            EndTime = sourceEnd,
            IsPublished = true
        });
        await context.SaveChangesAsync();

        var handler = new CommitCopyShiftsCommandHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new UnitOfWork(context)
        );

        // Run with CopyAssignments = true
        var cmd1 = new CommitCopyShiftsCommand(
            SiteId: 1,
            SourceDate: sourceDate,
            TargetDates: new List<DateOnly> { targetWithAssign },
            CopyAssignments: true
        );
        var res1 = await handler.Handle(cmd1, CancellationToken.None);
        Assert.True(res1.Success);

        // Run with CopyAssignments = false
        var cmd2 = new CommitCopyShiftsCommand(
            SiteId: 1,
            SourceDate: sourceDate,
            TargetDates: new List<DateOnly> { targetWithoutAssign },
            CopyAssignments: false
        );
        var res2 = await handler.Handle(cmd2, CancellationToken.None);
        Assert.True(res2.Success);

        var clonedWithAssign = await context.ShiftEntities
            .FirstAsync(s => s.StartTime >= new DateTimeOffset(targetWithAssign.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) && s.StartTime < new DateTimeOffset(targetWithAssign.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

        Assert.Equal(42, clonedWithAssign.EmployeeId);
        Assert.Equal(10, clonedWithAssign.JobRoleId);
        Assert.Equal(3, clonedWithAssign.ShiftTemplateId);
        Assert.True(clonedWithAssign.IsPublished);
        Assert.Equal(new TimeOnly(7, 30), TimeOnly.FromTimeSpan(clonedWithAssign.StartTime.TimeOfDay));
        Assert.Equal(new TimeOnly(16, 0), TimeOnly.FromTimeSpan(clonedWithAssign.EndTime.TimeOfDay));
        Assert.Equal(TimeSpan.FromHours(2), clonedWithAssign.StartTime.Offset);

        var clonedWithoutAssign = await context.ShiftEntities
            .FirstAsync(s => s.StartTime >= new DateTimeOffset(targetWithoutAssign.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) && s.StartTime < new DateTimeOffset(targetWithoutAssign.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

        Assert.Null(clonedWithoutAssign.EmployeeId);
        Assert.Equal(10, clonedWithoutAssign.JobRoleId);
        Assert.Equal(3, clonedWithoutAssign.ShiftTemplateId);
        Assert.True(clonedWithoutAssign.IsPublished);
    }

    [Fact]
    public async Task Controller_CommitCopyShifts_ReturnsOkWithResponse()
    {
        var expected = new CommitCopyShiftsResponseDto(
            Success: true,
            TotalDatesProcessed: 1,
            CopiedDatesCount: 1,
            SkippedDatesCount: 0,
            TotalShiftsCreated: 2,
            TotalShiftsReplaced: 0,
            CopiedDates: new List<DateOnly> { new DateOnly(2026, 9, 2) },
            SkippedDates: new List<DateOnly>(),
            Message: "Successfully copied 2 shifts across 1 dates."
        );

        var fakeMediator = new FakeMediator((req, ct) => Task.FromResult<object?>(expected));
        var controller = new ScheduleValidationController(fakeMediator);

        var requestDto = new CommitCopyShiftsRequestDto(
            SiteId: 1,
            SourceDate: new DateOnly(2026, 9, 1),
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 2) }
        );

        var actionResult = await controller.CommitCopyShifts(requestDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<CommitCopyShiftsResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.TotalShiftsCreated);
    }

    [Fact]
    public async Task Controller_CommitCopyShifts_ReturnsNotFound_WhenSiteMissing()
    {
        var fakeMediator = new FakeMediator((req, ct) => throw new KeyNotFoundException("Site with ID 999 not found."));
        var controller = new ScheduleValidationController(fakeMediator);

        var requestDto = new CommitCopyShiftsRequestDto(
            SiteId: 999,
            SourceDate: new DateOnly(2026, 9, 1),
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 2) }
        );

        var actionResult = await controller.CommitCopyShifts(requestDto, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task Controller_CommitCopyShifts_ReturnsBadRequest_WhenArgumentInvalid()
    {
        var fakeMediator = new FakeMediator((req, ct) => throw new ArgumentException("At least one target date must be specified."));
        var controller = new ScheduleValidationController(fakeMediator);

        var requestDto = new CommitCopyShiftsRequestDto(
            SiteId: 1,
            SourceDate: new DateOnly(2026, 9, 1)
        );

        var actionResult = await controller.CommitCopyShifts(requestDto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task Controller_CommitCopyShifts_ReturnsBadRequest_WhenSourceShiftsMissing()
    {
        var fakeMediator = new FakeMediator((req, ct) => throw new InvalidOperationException("No shifts found on source date to copy."));
        var controller = new ScheduleValidationController(fakeMediator);

        var requestDto = new CommitCopyShiftsRequestDto(
            SiteId: 1,
            SourceDate: new DateOnly(2026, 9, 1),
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 2) }
        );

        var actionResult = await controller.CommitCopyShifts(requestDto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    private class FakeMediator : ISender
    {
        private readonly Func<object, CancellationToken, Task<object?>> _handler;

        public FakeMediator(Func<object, CancellationToken, Task<object?>> handler)
        {
            _handler = handler;
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var res = await _handler(request!, cancellationToken);
            return (TResponse)res!;
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            return _handler(request!, cancellationToken);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            return _handler(request, cancellationToken);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<TResponse> CreateStream<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IStreamRequest<TResponse>
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
