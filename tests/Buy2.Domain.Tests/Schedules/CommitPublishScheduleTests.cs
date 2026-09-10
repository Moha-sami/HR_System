using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.CommitPublishSchedule;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Schedules;

public class CommitPublishScheduleTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static CommitPublishScheduleCommandHandler CreateHandler(Buy2DbContext context)
    {
        return new CommitPublishScheduleCommandHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context),
            new GenericRepository<Request>(context),
            new GenericRepository<Notification>(context),
            new UnitOfWork(context)
        );
    }

    [Fact]
    public async Task Handle_SiteNotFound_ThrowsKeyNotFoundException()
    {
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var command = new CommitPublishScheduleCommand(
            SiteId: 999,
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 10) }
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MissingDateSelection_ThrowsArgumentException()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Main Branch" });
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        var commandNull = new CommitPublishScheduleCommand(
            SiteId: 1,
            TargetDates: null,
            AllUnpublishedDays: false
        );

        var commandEmpty = new CommitPublishScheduleCommand(
            SiteId: 1,
            TargetDates: new List<DateOnly>(),
            AllUnpublishedDays: false
        );

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(commandNull, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(commandEmpty, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmptyShifts_ReturnsSuccessWithZeroProcessed()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Main Branch" });
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        var command = new CommitPublishScheduleCommand(
            SiteId: 1,
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 10) }
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(0, result.TotalShiftsProcessed);
        Assert.Equal(0, result.PublishedImmediatelyCount);
        Assert.Equal(0, result.PendingHrApprovalCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(result.PublishedShiftIds);
        Assert.Empty(result.PendingHrApprovalShiftIds);
        Assert.Empty(result.SkippedShiftIds);
    }

    [Fact]
    public async Task Handle_OvertimeShiftPublishedWithoutJustification_ThrowsArgumentException()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Main Branch" });
        var role = new JobRole { Id = 1, Title = "Cashier" };
        context.JobRoles.Add(role);

        var emp = new Employee
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@buy2.com",
            JobRoleId = 1,
            JobRole = role
        };
        context.Employees.Add(emp);

        var targetDate = new DateOnly(2026, 9, 10);
        var baseDate = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // 10 hour shift exceeds 8h daily limit -> Overtime
        var shift = new ShiftEntity
        {
            Id = 10,
            SiteId = 1,
            JobRoleId = 1,
            EmployeeId = emp.Id,
            StartTime = baseDate.AddHours(8),
            EndTime = baseDate.AddHours(18),
            IsPublished = false,
            Status = ShiftStatus.Draft
        };
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        var command = new CommitPublishScheduleCommand(
            SiteId: 1,
            TargetDates: new List<DateOnly> { targetDate },
            OvertimeJustification: null // Missing justification!
        );

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Overtime justification explanation is mandatory", ex.Message);
    }

    [Fact]
    public async Task Handle_CompliantShifts_PublishesImmediatelyAndCreatesNotifications()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Main Branch" });
        var role = new JobRole { Id = 1, Title = "Cashier" };
        context.JobRoles.Add(role);

        var emp = new Employee
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@buy2.com",
            JobRoleId = 1,
            JobRole = role
        };
        context.Employees.Add(emp);

        var targetDate = new DateOnly(2026, 9, 10);
        var baseDate = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // 8h shift matching employee role -> compliant
        var shift = new ShiftEntity
        {
            Id = 11,
            SiteId = 1,
            JobRoleId = 1,
            EmployeeId = emp.Id,
            StartTime = baseDate.AddHours(9),
            EndTime = baseDate.AddHours(17),
            IsPublished = false,
            Status = ShiftStatus.Draft
        };
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        var command = new CommitPublishScheduleCommand(
            SiteId: 1,
            TargetDates: new List<DateOnly> { targetDate }
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.TotalShiftsProcessed);
        Assert.Equal(1, result.PublishedImmediatelyCount);
        Assert.Equal(0, result.PendingHrApprovalCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Contains(11, result.PublishedShiftIds);

        var updatedShift = await context.ShiftEntities.FindAsync(11);
        Assert.NotNull(updatedShift);
        Assert.True(updatedShift.IsPublished);
        Assert.Equal(ShiftStatus.Published, updatedShift.Status);
        Assert.False(updatedShift.HasUnqualifiedOverride);

        var notification = await context.Notifications.FirstOrDefaultAsync(n => n.EmployeeId == emp.Id);
        Assert.NotNull(notification);
        Assert.Equal("Shift Published", notification.Title);
        Assert.Equal("Shift", notification.ReferenceType);
        Assert.Equal("11", notification.ReferenceId);
    }

    [Fact]
    public async Task Handle_UnqualifiedShift_Skipped_RemainsDraft()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Main Branch" });
        var role1 = new JobRole { Id = 1, Title = "Cashier" };
        var role2 = new JobRole { Id = 2, Title = "Manager" };
        context.JobRoles.AddRange(role1, role2);

        var emp = new Employee
        {
            Id = 1,
            FirstName = "Bob",
            LastName = "Jones",
            Email = "bob@buy2.com",
            JobRoleId = 1,
            JobRole = role1
        };
        context.Employees.Add(emp);

        var targetDate = new DateOnly(2026, 9, 10);
        var baseDate = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // Shift requires Role 2, employee has Role 1 -> Unqualified
        var shift = new ShiftEntity
        {
            Id = 12,
            SiteId = 1,
            JobRoleId = 2,
            EmployeeId = emp.Id,
            StartTime = baseDate.AddHours(9),
            EndTime = baseDate.AddHours(17),
            IsPublished = false,
            Status = ShiftStatus.Draft
        };
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        var command = new CommitPublishScheduleCommand(
            SiteId: 1,
            TargetDates: new List<DateOnly> { targetDate },
            ExceptionResolutions: new Dictionary<int, PublishExceptionDecision>
            {
                [12] = PublishExceptionDecision.Skip
            }
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.TotalShiftsProcessed);
        Assert.Equal(0, result.PublishedImmediatelyCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Contains(12, result.SkippedShiftIds);

        var updatedShift = await context.ShiftEntities.FindAsync(12);
        Assert.NotNull(updatedShift);
        Assert.False(updatedShift.IsPublished);
        Assert.Equal(ShiftStatus.Draft, updatedShift.Status);
    }

    [Fact]
    public async Task Handle_UnqualifiedShift_Published_PublishesWithOverrideAndNotification()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Main Branch" });
        var role1 = new JobRole { Id = 1, Title = "Cashier" };
        var role2 = new JobRole { Id = 2, Title = "Manager" };
        context.JobRoles.AddRange(role1, role2);

        var emp = new Employee
        {
            Id = 1,
            FirstName = "Bob",
            LastName = "Jones",
            Email = "bob@buy2.com",
            JobRoleId = 1,
            JobRole = role1
        };
        context.Employees.Add(emp);

        var targetDate = new DateOnly(2026, 9, 10);
        var baseDate = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var shift = new ShiftEntity
        {
            Id = 13,
            SiteId = 1,
            JobRoleId = 2,
            EmployeeId = emp.Id,
            StartTime = baseDate.AddHours(9),
            EndTime = baseDate.AddHours(17),
            IsPublished = false,
            Status = ShiftStatus.Draft
        };
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        var command = new CommitPublishScheduleCommand(
            SiteId: 1,
            TargetDates: new List<DateOnly> { targetDate },
            ExceptionResolutions: new Dictionary<int, PublishExceptionDecision>
            {
                [13] = PublishExceptionDecision.Publish
            }
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.PublishedImmediatelyCount);
        Assert.Contains(13, result.PublishedShiftIds);

        var updatedShift = await context.ShiftEntities.FindAsync(13);
        Assert.NotNull(updatedShift);
        Assert.True(updatedShift.IsPublished);
        Assert.Equal(ShiftStatus.Published, updatedShift.Status);
        Assert.True(updatedShift.HasUnqualifiedOverride);

        var notification = await context.Notifications.FirstOrDefaultAsync(n => n.EmployeeId == emp.Id);
        Assert.NotNull(notification);
        Assert.Equal("Shift Published with Override", notification.Title);
    }

    [Fact]
    public async Task Handle_OvertimeShift_WithJustification_TransitionsToPendingHrApprovalAndCreatesRequest()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Main Branch" });
        var role = new JobRole { Id = 1, Title = "Cashier" };
        context.JobRoles.Add(role);

        var emp = new Employee
        {
            Id = 1,
            FirstName = "Charlie",
            LastName = "Brown",
            Email = "charlie@buy2.com",
            JobRoleId = 1,
            JobRole = role
        };
        context.Employees.Add(emp);

        var targetDate = new DateOnly(2026, 9, 10);
        var baseDate = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // 9h shift -> Daily overtime
        var shift = new ShiftEntity
        {
            Id = 14,
            SiteId = 1,
            JobRoleId = 1,
            EmployeeId = emp.Id,
            StartTime = baseDate.AddHours(8),
            EndTime = baseDate.AddHours(17),
            IsPublished = false,
            Status = ShiftStatus.Draft
        };
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        var command = new CommitPublishScheduleCommand(
            SiteId: 1,
            TargetDates: new List<DateOnly> { targetDate },
            OvertimeJustification: "Peak holiday season coverage"
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(0, result.PublishedImmediatelyCount);
        Assert.Equal(1, result.PendingHrApprovalCount);
        Assert.Contains(14, result.PendingHrApprovalShiftIds);

        var updatedShift = await context.ShiftEntities.FindAsync(14);
        Assert.NotNull(updatedShift);
        Assert.False(updatedShift.IsPublished);
        Assert.Equal(ShiftStatus.PendingHrApproval, updatedShift.Status);

        var hrRequest = await context.Requests.FirstOrDefaultAsync(r => r.EmployeeId == emp.Id);
        Assert.NotNull(hrRequest);
        Assert.Equal("Pending HR Approval", hrRequest.Status);
        Assert.Equal("Peak holiday season coverage", hrRequest.Reason);
        Assert.Equal(shift.StartTime.UtcDateTime, hrRequest.StartDate);
        Assert.Equal(shift.EndTime.UtcDateTime, hrRequest.EndDate);
    }

    [Fact]
    public async Task Handle_OvertimeShift_Skipped_RemainsDraftAndNoRequestCreated()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Main Branch" });
        var role = new JobRole { Id = 1, Title = "Cashier" };
        context.JobRoles.Add(role);

        var emp = new Employee
        {
            Id = 1,
            FirstName = "Charlie",
            LastName = "Brown",
            Email = "charlie@buy2.com",
            JobRoleId = 1,
            JobRole = role
        };
        context.Employees.Add(emp);

        var targetDate = new DateOnly(2026, 9, 10);
        var baseDate = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var shift = new ShiftEntity
        {
            Id = 15,
            SiteId = 1,
            JobRoleId = 1,
            EmployeeId = emp.Id,
            StartTime = baseDate.AddHours(8),
            EndTime = baseDate.AddHours(18),
            IsPublished = false,
            Status = ShiftStatus.Draft
        };
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Skipped overtime shift -> no justification needed since not publishing!
        var command = new CommitPublishScheduleCommand(
            SiteId: 1,
            TargetDates: new List<DateOnly> { targetDate },
            ExceptionResolutions: new Dictionary<int, PublishExceptionDecision>
            {
                [15] = PublishExceptionDecision.Skip
            },
            OvertimeJustification: null
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.SkippedCount);
        Assert.Contains(15, result.SkippedShiftIds);

        var updatedShift = await context.ShiftEntities.FindAsync(15);
        Assert.NotNull(updatedShift);
        Assert.False(updatedShift.IsPublished);
        Assert.Equal(ShiftStatus.Draft, updatedShift.Status);

        var requests = await context.Requests.ToListAsync();
        Assert.Empty(requests);
    }

    [Fact]
    public async Task Handle_SplitPublication_MixedBatchHandledAccurately()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Main Branch" });
        var role1 = new JobRole { Id = 1, Title = "Role 1" };
        var role2 = new JobRole { Id = 2, Title = "Role 2" };
        context.JobRoles.AddRange(role1, role2);

        var emp1 = new Employee { Id = 1, FirstName = "Emp1", LastName = "One", Email = "e1@b.com", JobRoleId = 1, JobRole = role1 };
        var emp2 = new Employee { Id = 2, FirstName = "Emp2", LastName = "Two", Email = "e2@b.com", JobRoleId = 1, JobRole = role1 };
        var emp3 = new Employee { Id = 3, FirstName = "Emp3", LastName = "Three", Email = "e3@b.com", JobRoleId = 1, JobRole = role1 };
        var emp4 = new Employee { Id = 4, FirstName = "Emp4", LastName = "Four", Email = "e4@b.com", JobRoleId = 1, JobRole = role1 };
        context.Employees.AddRange(emp1, emp2, emp3, emp4);

        var targetDate = new DateOnly(2026, 9, 10);
        var baseDate = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // Shift 21: Compliant 8h
        var s1 = new ShiftEntity { Id = 21, SiteId = 1, JobRoleId = 1, EmployeeId = 1, StartTime = baseDate.AddHours(9), EndTime = baseDate.AddHours(17), IsPublished = false };
        // Shift 22: Unqualified (Role 2 vs emp Role 1), published
        var s2 = new ShiftEntity { Id = 22, SiteId = 1, JobRoleId = 2, EmployeeId = 2, StartTime = baseDate.AddHours(9), EndTime = baseDate.AddHours(17), IsPublished = false };
        // Shift 23: Overtime 10h, published
        var s3 = new ShiftEntity { Id = 23, SiteId = 1, JobRoleId = 1, EmployeeId = 3, StartTime = baseDate.AddHours(8), EndTime = baseDate.AddHours(18), IsPublished = false };
        // Shift 24: Unqualified, skipped
        var s4 = new ShiftEntity { Id = 24, SiteId = 1, JobRoleId = 2, EmployeeId = 4, StartTime = baseDate.AddHours(9), EndTime = baseDate.AddHours(17), IsPublished = false };

        context.ShiftEntities.AddRange(s1, s2, s3, s4);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        var command = new CommitPublishScheduleCommand(
            SiteId: 1,
            TargetDates: new List<DateOnly> { targetDate },
            ExceptionResolutions: new Dictionary<int, PublishExceptionDecision>
            {
                [22] = PublishExceptionDecision.Publish,
                [23] = PublishExceptionDecision.Publish,
                [24] = PublishExceptionDecision.Skip
            },
            OvertimeJustification: "Special rush requirement"
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(4, result.TotalShiftsProcessed);
        Assert.Equal(2, result.PublishedImmediatelyCount);
        Assert.Equal(1, result.PendingHrApprovalCount);
        Assert.Equal(1, result.SkippedCount);

        Assert.Contains(21, result.PublishedShiftIds);
        Assert.Contains(22, result.PublishedShiftIds);
        Assert.Contains(23, result.PendingHrApprovalShiftIds);
        Assert.Contains(24, result.SkippedShiftIds);

        var checkS1 = await context.ShiftEntities.FindAsync(21);
        var checkS2 = await context.ShiftEntities.FindAsync(22);
        var checkS3 = await context.ShiftEntities.FindAsync(23);
        var checkS4 = await context.ShiftEntities.FindAsync(24);

        Assert.True(checkS1!.IsPublished);
        Assert.Equal(ShiftStatus.Published, checkS1.Status);
        Assert.False(checkS1.HasUnqualifiedOverride);

        Assert.True(checkS2!.IsPublished);
        Assert.Equal(ShiftStatus.Published, checkS2.Status);
        Assert.True(checkS2.HasUnqualifiedOverride);

        Assert.False(checkS3!.IsPublished);
        Assert.Equal(ShiftStatus.PendingHrApproval, checkS3.Status);

        Assert.False(checkS4!.IsPublished);
        Assert.Equal(ShiftStatus.Draft, checkS4.Status);
    }

    [Fact]
    public async Task Handle_RoleFiltering_OnlyProcessesSpecifiedRoles()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Main Branch" });
        var role1 = new JobRole { Id = 1, Title = "Cashier" };
        var role2 = new JobRole { Id = 2, Title = "Manager" };
        context.JobRoles.AddRange(role1, role2);

        var targetDate = new DateOnly(2026, 9, 10);
        var baseDate = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var shiftRole1 = new ShiftEntity { Id = 31, SiteId = 1, JobRoleId = 1, StartTime = baseDate.AddHours(9), EndTime = baseDate.AddHours(17), IsPublished = false };
        var shiftRole2 = new ShiftEntity { Id = 32, SiteId = 1, JobRoleId = 2, StartTime = baseDate.AddHours(9), EndTime = baseDate.AddHours(17), IsPublished = false };
        context.ShiftEntities.AddRange(shiftRole1, shiftRole2);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        var command = new CommitPublishScheduleCommand(
            SiteId: 1,
            TargetDates: new List<DateOnly> { targetDate },
            TargetRoleIds: new List<int> { 1 } // Only Role 1
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.TotalShiftsProcessed);
        Assert.Contains(31, result.PublishedShiftIds);

        var s1 = await context.ShiftEntities.FindAsync(31);
        var s2 = await context.ShiftEntities.FindAsync(32);
        Assert.True(s1!.IsPublished);
        Assert.False(s2!.IsPublished);
    }

    [Fact]
    public async Task Handle_AllUnpublishedDays_ProcessesAcrossMultipleDates()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Main Branch" });
        var role = new JobRole { Id = 1, Title = "Cashier" };
        context.JobRoles.Add(role);

        var d1 = new DateOnly(2026, 9, 10);
        var d2 = new DateOnly(2026, 9, 12);
        var base1 = new DateTimeOffset(d1.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var base2 = new DateTimeOffset(d2.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var s1 = new ShiftEntity { Id = 41, SiteId = 1, JobRoleId = 1, StartTime = base1.AddHours(9), EndTime = base1.AddHours(17), IsPublished = false };
        var s2 = new ShiftEntity { Id = 42, SiteId = 1, JobRoleId = 1, StartTime = base2.AddHours(9), EndTime = base2.AddHours(17), IsPublished = false };
        context.ShiftEntities.AddRange(s1, s2);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        var command = new CommitPublishScheduleCommand(
            SiteId: 1,
            AllUnpublishedDays: true
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.TotalShiftsProcessed);
        Assert.Contains(41, result.PublishedShiftIds);
        Assert.Contains(42, result.PublishedShiftIds);
    }

    [Fact]
    public async Task Controller_CommitPublishSchedule_ReturnsOk_WhenSuccessful()
    {
        var expectedResponse = new CommitPublishScheduleResponseDto(
            Success: true,
            SiteId: 1,
            TotalShiftsProcessed: 2,
            PublishedImmediatelyCount: 1,
            PendingHrApprovalCount: 1,
            SkippedCount: 0,
            PublishedShiftIds: new List<int> { 1 },
            PendingHrApprovalShiftIds: new List<int> { 2 },
            SkippedShiftIds: new List<int>(),
            Message: "Success"
        );

        var fakeMediator = new FakeMediator((req, ct) => Task.FromResult<object?>(expectedResponse));
        var controller = new ScheduleValidationController(fakeMediator);

        var requestDto = new CommitPublishScheduleRequestDto(
            SiteId: 1,
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 10) }
        );

        var actionResult = await controller.CommitPublishSchedule(requestDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var data = Assert.IsType<CommitPublishScheduleResponseDto>(okResult.Value);
        Assert.True(data.Success);
        Assert.Equal(2, data.TotalShiftsProcessed);
    }

    [Fact]
    public async Task Controller_CommitPublishSchedule_ReturnsNotFound_WhenSiteMissing()
    {
        var fakeMediator = new FakeMediator((req, ct) => throw new KeyNotFoundException("Site with ID 999 not found."));
        var controller = new ScheduleValidationController(fakeMediator);

        var requestDto = new CommitPublishScheduleRequestDto(
            SiteId: 999,
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 10) }
        );

        var actionResult = await controller.CommitPublishSchedule(requestDto, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task Controller_CommitPublishSchedule_ReturnsBadRequest_WhenArgumentInvalid()
    {
        var fakeMediator = new FakeMediator((req, ct) => throw new ArgumentException("Either TargetDates or AllUnpublishedDays must be specified."));
        var controller = new ScheduleValidationController(fakeMediator);

        var requestDto = new CommitPublishScheduleRequestDto(
            SiteId: 1,
            TargetDates: null,
            AllUnpublishedDays: false
        );

        var actionResult = await controller.CommitPublishSchedule(requestDto, CancellationToken.None);

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
