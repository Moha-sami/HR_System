using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.PreflightPublishSchedule;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Schedules;

public class PreflightPublishScheduleTests
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
        var handler = new PreflightPublishScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var query = new PreflightPublishScheduleQuery(
            SiteId: 999,
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 10) }
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NeitherTargetDatesNorAllUnpublishedDays_ThrowsArgumentException()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Main Branch" });
        await context.SaveChangesAsync();

        var handler = new PreflightPublishScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var queryNull = new PreflightPublishScheduleQuery(
            SiteId: 1,
            TargetDates: null,
            AllUnpublishedDays: false
        );

        var queryEmpty = new PreflightPublishScheduleQuery(
            SiteId: 1,
            TargetDates: new List<DateOnly>(),
            AllUnpublishedDays: false
        );

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(queryNull, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(queryEmpty, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmptyUnpublishedShifts_ReturnsZeroScannedAndCanPublishImmediately()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Main Branch" });
        await context.SaveChangesAsync();

        var handler = new PreflightPublishScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var targetDate = new DateOnly(2026, 9, 10);
        var query = new PreflightPublishScheduleQuery(
            SiteId: 1,
            TargetDates: new List<DateOnly> { targetDate }
        );

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, result.SiteId);
        Assert.Equal(0, result.TotalUnpublishedShiftsScanned);
        Assert.Equal(1, result.TotalDatesScanned);
        Assert.Contains(targetDate, result.ScannedDates);
        Assert.Empty(result.UnqualifiedAssignees);
        Assert.Empty(result.OvertimeViolations);
        Assert.Equal(0, result.UnqualifiedCount);
        Assert.Equal(0, result.OvertimeCount);
        Assert.False(result.HasExceptions);
        Assert.True(result.CanPublishImmediately);
    }

    [Fact]
    public async Task Handle_CompliantShifts_ReturnsCleanReportAndCanPublishImmediately()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 5, Title = "Cashier" };
        var employee = new Employee
        {
            Id = 10,
            FirstName = "Alice",
            LastName = "Smith",
            JobRoleId = 5,
            JobRole = role
        };

        var shiftDate = new DateOnly(2026, 9, 10);
        var start = new DateTimeOffset(shiftDate.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
        var end = start.AddHours(8);

        var shift = new ShiftEntity
        {
            Id = 101,
            SiteId = 1,
            JobRoleId = 5,
            JobRole = role,
            EmployeeId = 10,
            StartTime = start,
            EndTime = end,
            IsPublished = false
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = new PreflightPublishScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var query = new PreflightPublishScheduleQuery(
            SiteId: 1,
            TargetDates: new List<DateOnly> { shiftDate }
        );

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, result.TotalUnpublishedShiftsScanned);
        Assert.Equal(0, result.UnqualifiedCount);
        Assert.Equal(0, result.OvertimeCount);
        Assert.False(result.HasExceptions);
        Assert.True(result.CanPublishImmediately);
    }

    [Fact]
    public async Task Handle_UnqualifiedAssignee_DetectedInUnqualifiedAssignees()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var shiftRole = new JobRole { Id = 5, Title = "Cashier" };
        var empRole = new JobRole { Id = 8, Title = "Stock Clerk" };

        var employee = new Employee
        {
            Id = 10,
            FirstName = "Bob",
            LastName = "Jones",
            JobRoleId = 8,
            JobRole = empRole
        };

        var shiftDate = new DateOnly(2026, 9, 10);
        var start = new DateTimeOffset(shiftDate.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
        var end = start.AddHours(8);

        var shift = new ShiftEntity
        {
            Id = 102,
            SiteId = 1,
            JobRoleId = 5,
            JobRole = shiftRole,
            EmployeeId = 10,
            StartTime = start,
            EndTime = end,
            IsPublished = false
        };

        context.Sites.Add(site);
        context.JobRoles.AddRange(shiftRole, empRole);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = new PreflightPublishScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var query = new PreflightPublishScheduleQuery(
            SiteId: 1,
            TargetDates: new List<DateOnly> { shiftDate }
        );

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, result.TotalUnpublishedShiftsScanned);
        Assert.Equal(1, result.UnqualifiedCount);
        Assert.Equal(0, result.OvertimeCount);
        Assert.True(result.HasExceptions);
        Assert.False(result.CanPublishImmediately);

        var ex = Assert.Single(result.UnqualifiedAssignees);
        Assert.Equal(102, ex.ShiftId);
        Assert.Equal(10, ex.EmployeeId);
        Assert.Equal("Bob Jones", ex.EmployeeName);
        Assert.Equal(5, ex.RequiredRoleId);
        Assert.Equal("Cashier", ex.RequiredRoleTitle);
        Assert.Equal(8, ex.EmployeeRoleId);
        Assert.Equal("Stock Clerk", ex.EmployeeRoleTitle);
        Assert.Equal(shiftDate, ex.Date);
        Assert.Equal(new TimeSpan(8, 0, 0), ex.StartTime);
        Assert.Equal(new TimeSpan(16, 0, 0), ex.EndTime);
        Assert.Contains("08:00 AM", ex.FormattedTime);
        Assert.Contains("04:00 PM", ex.FormattedTime);
    }

    [Fact]
    public async Task Handle_OvertimeViolation_DailyShiftOver8Hours_DetectedInOvertimeViolations()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 5, Title = "Cashier" };
        var employee = new Employee
        {
            Id = 10,
            FirstName = "Charlie",
            LastName = "Brown",
            JobRoleId = 5,
            JobRole = role
        };

        var shiftDate = new DateOnly(2026, 9, 10);
        var start = new DateTimeOffset(shiftDate.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
        var end = start.AddHours(10); // 10h shift > 8h

        var shift = new ShiftEntity
        {
            Id = 103,
            SiteId = 1,
            JobRoleId = 5,
            JobRole = role,
            EmployeeId = 10,
            StartTime = start,
            EndTime = end,
            IsPublished = false
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = new PreflightPublishScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var query = new PreflightPublishScheduleQuery(
            SiteId: 1,
            TargetDates: new List<DateOnly> { shiftDate }
        );

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, result.TotalUnpublishedShiftsScanned);
        Assert.Equal(0, result.UnqualifiedCount);
        Assert.Equal(1, result.OvertimeCount);
        Assert.True(result.HasExceptions);
        Assert.False(result.CanPublishImmediately);

        var ot = Assert.Single(result.OvertimeViolations);
        Assert.Equal(103, ot.ShiftId);
        Assert.Equal(10, ot.EmployeeId);
        Assert.Equal("Charlie Brown", ot.EmployeeName);
        Assert.Equal(10m, ot.ShiftHours);
        Assert.Equal(10m, ot.TotalWeeklyHours);
        Assert.Equal(2.0m, ot.ProjectedOtHours); // 10 - 8 = 2
        Assert.Contains("8.0h", ot.ViolationReason);
    }

    [Fact]
    public async Task Handle_OvertimeViolation_WeeklyHoursOverThreshold_DetectedInOvertimeViolations()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 5, Title = "Cashier" };
        var employee = new Employee
        {
            Id = 10,
            FirstName = "Diana",
            LastName = "Prince",
            JobRoleId = 5,
            JobRole = role
        };

        // 2026-09-07 is Monday
        var monday = new DateOnly(2026, 9, 7);
        var tuesday = new DateOnly(2026, 9, 8);
        var wednesday = new DateOnly(2026, 9, 9);
        var thursday = new DateOnly(2026, 9, 10);
        var friday = new DateOnly(2026, 9, 11);

        // Published shifts: 4 shifts of 8h = 32h
        var shifts = new List<ShiftEntity>();
        var dates = new[] { monday, tuesday, wednesday, thursday };
        for (int i = 0; i < dates.Length; i++)
        {
            var start = new DateTimeOffset(dates[i].ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
            shifts.Add(new ShiftEntity
            {
                Id = 200 + i,
                SiteId = 1,
                JobRoleId = 5,
                JobRole = role,
                EmployeeId = 10,
                StartTime = start,
                EndTime = start.AddHours(8),
                IsPublished = true
            });
        }

        // Unpublished shift on Friday of 9h: total = 32 + 9 = 41h > 40h
        var friStart = new DateTimeOffset(friday.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
        var unpubShift = new ShiftEntity
        {
            Id = 205,
            SiteId = 1,
            JobRoleId = 5,
            JobRole = role,
            EmployeeId = 10,
            StartTime = friStart,
            EndTime = friStart.AddHours(9),
            IsPublished = false
        };
        shifts.Add(unpubShift);

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.AddRange(shifts);
        await context.SaveChangesAsync();

        var handler = new PreflightPublishScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var query = new PreflightPublishScheduleQuery(
            SiteId: 1,
            TargetDates: new List<DateOnly> { friday }
        );

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, result.TotalUnpublishedShiftsScanned);
        Assert.Equal(1, result.OvertimeCount);
        var ot = Assert.Single(result.OvertimeViolations);
        Assert.Equal(205, ot.ShiftId);
        Assert.Equal(9m, ot.ShiftHours);
        Assert.Equal(41m, ot.TotalWeeklyHours);
        Assert.True(ot.ProjectedOtHours >= 1.0m);
    }

    [Fact]
    public async Task Handle_OvertimeViolation_CustomPayrollThreshold_RespectsPayrollThreshold()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 5, Title = "Cashier" };
        var employee = new Employee
        {
            Id = 10,
            FirstName = "Eva",
            LastName = "Green",
            JobRoleId = 5,
            JobRole = role,
            PayrollProfile = new PayrollProfile
            {
                EmployeeId = 10,
                OvertimeThresholdHours = 30m
            }
        };

        var monday = new DateOnly(2026, 9, 7);
        var tuesday = new DateOnly(2026, 9, 8);
        var wednesday = new DateOnly(2026, 9, 9);
        var thursday = new DateOnly(2026, 9, 10);

        // 3 shifts of 8h = 24h
        var shifts = new List<ShiftEntity>();
        var dates = new[] { monday, tuesday, wednesday };
        for (int i = 0; i < dates.Length; i++)
        {
            var start = new DateTimeOffset(dates[i].ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
            shifts.Add(new ShiftEntity
            {
                Id = 300 + i,
                SiteId = 1,
                JobRoleId = 5,
                JobRole = role,
                EmployeeId = 10,
                StartTime = start,
                EndTime = start.AddHours(8),
                IsPublished = true
            });
        }

        // Unpublished shift on Thursday of 8h: total = 24 + 8 = 32h > 30h threshold
        var thurStart = new DateTimeOffset(thursday.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
        shifts.Add(new ShiftEntity
        {
            Id = 305,
            SiteId = 1,
            JobRoleId = 5,
            JobRole = role,
            EmployeeId = 10,
            StartTime = thurStart,
            EndTime = thurStart.AddHours(8),
            IsPublished = false
        });

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.AddRange(shifts);
        await context.SaveChangesAsync();

        var handler = new PreflightPublishScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var query = new PreflightPublishScheduleQuery(
            SiteId: 1,
            TargetDates: new List<DateOnly> { thursday }
        );

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, result.TotalUnpublishedShiftsScanned);
        Assert.Equal(1, result.OvertimeCount);
        var ot = Assert.Single(result.OvertimeViolations);
        Assert.Equal(32m, ot.TotalWeeklyHours);
        Assert.Equal(2.0m, ot.ProjectedOtHours); // 32 - 30 = 2
        Assert.Contains("30", ot.ViolationReason);
    }

    [Fact]
    public async Task Handle_CombinedExceptions_BothTablesPopulatedAndCannotPublishImmediately()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var cashierRole = new JobRole { Id = 5, Title = "Cashier" };
        var stockRole = new JobRole { Id = 6, Title = "Stock Clerk" };

        var empUnqualified = new Employee
        {
            Id = 10,
            FirstName = "Frank",
            LastName = "Miller",
            JobRoleId = 6,
            JobRole = stockRole
        };

        var empOvertime = new Employee
        {
            Id = 11,
            FirstName = "Grace",
            LastName = "Hopper",
            JobRoleId = 5,
            JobRole = cashierRole
        };

        var date = new DateOnly(2026, 9, 10);
        var start1 = new DateTimeOffset(date.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
        var start2 = new DateTimeOffset(date.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);

        var shift1 = new ShiftEntity
        {
            Id = 401,
            SiteId = 1,
            JobRoleId = 5, // Requires Cashier, but Frank is Stock Clerk
            JobRole = cashierRole,
            EmployeeId = 10,
            StartTime = start1,
            EndTime = start1.AddHours(8),
            IsPublished = false
        };

        var shift2 = new ShiftEntity
        {
            Id = 402,
            SiteId = 1,
            JobRoleId = 5,
            JobRole = cashierRole,
            EmployeeId = 11,
            StartTime = start2,
            EndTime = start2.AddHours(10), // Overtime > 8h
            IsPublished = false
        };

        context.Sites.Add(site);
        context.JobRoles.AddRange(cashierRole, stockRole);
        context.Employees.AddRange(empUnqualified, empOvertime);
        context.ShiftEntities.AddRange(shift1, shift2);
        await context.SaveChangesAsync();

        var handler = new PreflightPublishScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var query = new PreflightPublishScheduleQuery(
            SiteId: 1,
            TargetDates: new List<DateOnly> { date }
        );

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.TotalUnpublishedShiftsScanned);
        Assert.Equal(1, result.UnqualifiedCount);
        Assert.Equal(1, result.OvertimeCount);
        Assert.True(result.HasExceptions);
        Assert.False(result.CanPublishImmediately);
        Assert.NotEmpty(result.UnqualifiedAssignees);
        Assert.NotEmpty(result.OvertimeViolations);
    }

    [Fact]
    public async Task Handle_RoleFiltering_OnlyInspectsSpecifiedRoleIds()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var cashierRole = new JobRole { Id = 5, Title = "Cashier" };
        var stockRole = new JobRole { Id = 6, Title = "Stock Clerk" };

        var empStock = new Employee
        {
            Id = 10,
            FirstName = "Hank",
            LastName = "Pym",
            JobRoleId = 6,
            JobRole = stockRole
        };

        var date = new DateOnly(2026, 9, 10);
        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);

        // Shift for Cashier with unqualified employee
        var shiftCashier = new ShiftEntity
        {
            Id = 501,
            SiteId = 1,
            JobRoleId = 5,
            JobRole = cashierRole,
            EmployeeId = 10, // Unqualified for Cashier
            StartTime = start,
            EndTime = start.AddHours(8),
            IsPublished = false
        };

        // Shift for Stock Clerk with qualified employee
        var shiftStock = new ShiftEntity
        {
            Id = 502,
            SiteId = 1,
            JobRoleId = 6,
            JobRole = stockRole,
            EmployeeId = 10, // Qualified for Stock Clerk
            StartTime = start,
            EndTime = start.AddHours(8),
            IsPublished = false
        };

        context.Sites.Add(site);
        context.JobRoles.AddRange(cashierRole, stockRole);
        context.Employees.Add(empStock);
        context.ShiftEntities.AddRange(shiftCashier, shiftStock);
        await context.SaveChangesAsync();

        var handler = new PreflightPublishScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Target ONLY Stock Clerk role (Id = 6)
        var query = new PreflightPublishScheduleQuery(
            SiteId: 1,
            TargetDates: new List<DateOnly> { date },
            TargetRoleIds: new List<int> { 6 }
        );

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, result.TotalUnpublishedShiftsScanned);
        Assert.Equal(0, result.UnqualifiedCount);
        Assert.Equal(0, result.OvertimeCount);
        Assert.True(result.CanPublishImmediately);
    }

    [Fact]
    public async Task Handle_AllUnpublishedDaysFlag_ScansAcrossMultipleDates()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 5, Title = "Cashier" };

        var date1 = new DateOnly(2026, 9, 1);
        var date2 = new DateOnly(2026, 9, 3);
        var date3 = new DateOnly(2026, 9, 5);

        var start1 = new DateTimeOffset(date1.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
        var start2 = new DateTimeOffset(date2.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
        var start3 = new DateTimeOffset(date3.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.ShiftEntities.AddRange(
            new ShiftEntity { Id = 601, SiteId = 1, JobRoleId = 5, StartTime = start1, EndTime = start1.AddHours(8), IsPublished = false },
            new ShiftEntity { Id = 602, SiteId = 1, JobRoleId = 5, StartTime = start2, EndTime = start2.AddHours(8), IsPublished = false },
            new ShiftEntity { Id = 603, SiteId = 1, JobRoleId = 5, StartTime = start3, EndTime = start3.AddHours(8), IsPublished = false }
        );
        await context.SaveChangesAsync();

        var handler = new PreflightPublishScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var query = new PreflightPublishScheduleQuery(
            SiteId: 1,
            AllUnpublishedDays: true
        );

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(3, result.TotalUnpublishedShiftsScanned);
        Assert.Equal(3, result.TotalDatesScanned);
        Assert.Equal(new List<DateOnly> { date1, date2, date3 }, result.ScannedDates);
        Assert.True(result.CanPublishImmediately);
    }

    [Fact]
    public async Task Handle_UnassignedUnpublishedShifts_ScannedWithoutExceptions()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 5, Title = "Cashier" };
        var date = new DateOnly(2026, 9, 10);
        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);

        var unassignedShift = new ShiftEntity
        {
            Id = 701,
            SiteId = 1,
            JobRoleId = 5,
            JobRole = role,
            EmployeeId = null,
            StartTime = start,
            EndTime = start.AddHours(8),
            IsPublished = false
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.ShiftEntities.Add(unassignedShift);
        await context.SaveChangesAsync();

        var handler = new PreflightPublishScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var query = new PreflightPublishScheduleQuery(
            SiteId: 1,
            TargetDates: new List<DateOnly> { date }
        );

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, result.TotalUnpublishedShiftsScanned);
        Assert.Equal(0, result.UnqualifiedCount);
        Assert.Equal(0, result.OvertimeCount);
        Assert.True(result.CanPublishImmediately);
    }

    [Fact]
    public async Task Controller_PreflightPublishSchedule_ReturnsOk_WhenValid()
    {
        var fakeResponse = new PreflightPublishScheduleResponseDto(
            SiteId: 1,
            TotalUnpublishedShiftsScanned: 0,
            TotalDatesScanned: 1,
            ScannedDates: new List<DateOnly> { new DateOnly(2026, 9, 10) },
            UnqualifiedAssignees: new List<UnqualifiedAssigneeExceptionDto>(),
            OvertimeViolations: new List<OvertimeViolationExceptionDto>(),
            UnqualifiedCount: 0,
            OvertimeCount: 0,
            HasExceptions: false,
            CanPublishImmediately: true
        );

        var fakeMediator = new FakeMediator((req, ct) => Task.FromResult<object?>(fakeResponse));
        var controller = new ScheduleValidationController(fakeMediator);

        var requestDto = new PreflightPublishScheduleRequestDto(
            SiteId: 1,
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 10) }
        );

        var actionResult = await controller.PreflightPublishSchedule(requestDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var model = Assert.IsType<PreflightPublishScheduleResponseDto>(okResult.Value);
        Assert.True(model.CanPublishImmediately);
    }

    [Fact]
    public async Task Controller_PreflightPublishSchedule_ReturnsNotFound_WhenSiteNotFound()
    {
        var fakeMediator = new FakeMediator((req, ct) => throw new KeyNotFoundException("Site with ID 999 not found."));
        var controller = new ScheduleValidationController(fakeMediator);

        var requestDto = new PreflightPublishScheduleRequestDto(
            SiteId: 999,
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 10) }
        );

        var actionResult = await controller.PreflightPublishSchedule(requestDto, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task Controller_PreflightPublishSchedule_ReturnsBadRequest_WhenArgumentInvalid()
    {
        var fakeMediator = new FakeMediator((req, ct) => throw new ArgumentException("Either TargetDates or AllUnpublishedDays must be specified."));
        var controller = new ScheduleValidationController(fakeMediator);

        var requestDto = new PreflightPublishScheduleRequestDto(
            SiteId: 1,
            TargetDates: null,
            AllUnpublishedDays: false
        );

        var actionResult = await controller.PreflightPublishSchedule(requestDto, CancellationToken.None);

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
