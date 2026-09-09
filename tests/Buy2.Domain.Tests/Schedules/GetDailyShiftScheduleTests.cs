using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.GetDailyShiftSchedule;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Schedules;

public class GetDailyShiftScheduleTests
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
    public async Task Handle_EmptyDaySchedule_ReturnsZeroCost_NotDayOff_AndNoAllocationsBadge()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9); // Wednesday

        var site = new Site
        {
            Id = 1,
            SiteName = "Main Branch",
            OperationalHours = new List<SiteOperationalHour>
            {
                new() { DayOfWeek = DayOfWeek.Wednesday, IsOpen = true, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(17, 0) }
            }
        };

        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var handler = new GetDailyShiftScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetDailyShiftScheduleQuery(1, targetDate), CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SiteId);
        Assert.Equal("Main Branch", result.SiteName);
        Assert.Equal(targetDate, result.Date);
        Assert.False(result.IsDayOff);
        Assert.Equal(0m, result.TotalEstimatedLaborCost);
        Assert.Equal(7, result.WeekCalendarStrip.Count);

        var targetBadge = result.WeekCalendarStrip.First(b => b.Date == targetDate);
        Assert.True(targetBadge.IsSelected);
        Assert.False(targetBadge.IsDayOff);
        Assert.Equal(WeekDayCalendarStatus.NoAllocations, targetBadge.Status);

        Assert.NotEmpty(result.HourlyTimeline);
        Assert.All(result.HourlyTimeline, interval => Assert.Empty(interval.Blocks));
    }

    [Fact]
    public async Task Handle_CoveredAndPublishedSchedule_CalculatesLaborCostCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9); // Wednesday
        var startDateTime = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
        var endDateTime = startDateTime.AddHours(8); // 8-hour shift

        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 5, Title = "Cashier" };

        var employee = new Employee
        {
            Id = 10,
            FirstName = "John",
            LastName = "Doe",
            ProfilePhotoUrl = "https://avatar.test/johndoe.png",
            JobRoleId = 5,
            PayrollProfile = new PayrollProfile
            {
                SalaryType = "Hourly",
                PaymentAmount = 25.50m
            }
        };

        var shift = new ShiftEntity
        {
            Id = 100,
            SiteId = 1,
            JobRoleId = 5,
            JobRole = role,
            EmployeeId = 10,
            StartTime = startDateTime,
            EndTime = endDateTime,
            IsPublished = true
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = new GetDailyShiftScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetDailyShiftScheduleQuery(1, targetDate), CancellationToken.None);

        // Assert
        // 8 hours * 25.50 = 204.00
        Assert.Equal(204.00m, result.TotalEstimatedLaborCost);

        var targetBadge = result.WeekCalendarStrip.First(b => b.Date == targetDate);
        Assert.Equal(WeekDayCalendarStatus.CoveredAndPublished, targetBadge.Status);

        var firstInterval = result.HourlyTimeline.First(i => i.StartHour == new TimeOnly(9, 0));
        var block = Assert.Single(firstInterval.Blocks);
        Assert.Equal(100, block.ShiftId);
        Assert.Equal("Cashier", block.RoleTitle);
        Assert.Equal("John Doe", block.EmployeeName);
        Assert.Equal("https://avatar.test/johndoe.png", block.EmployeeAvatarUrl);
        Assert.Equal("#10B981", block.StatusColorCode); // Green / Covered
        Assert.True(block.IsPublished);
    }

    [Fact]
    public async Task Handle_OpenUnassignedSlots_GetOrangeColorCode_AndMissingResourcesStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9);
        var startDateTime = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);
        var endDateTime = startDateTime.AddHours(4);

        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 3, Title = "Security" };

        var shift = new ShiftEntity
        {
            Id = 201,
            SiteId = 1,
            JobRoleId = 3,
            JobRole = role,
            EmployeeId = null,
            StartTime = startDateTime,
            EndTime = endDateTime,
            IsPublished = true
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = new GetDailyShiftScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetDailyShiftScheduleQuery(1, targetDate), CancellationToken.None);

        // Assert
        Assert.Equal(0m, result.TotalEstimatedLaborCost);

        var targetBadge = result.WeekCalendarStrip.First(b => b.Date == targetDate);
        Assert.Equal(WeekDayCalendarStatus.MissingResourcesOrUnpublished, targetBadge.Status);

        var interval = result.HourlyTimeline.First(i => i.StartHour == new TimeOnly(10, 0));
        var block = Assert.Single(interval.Blocks);
        Assert.Null(block.EmployeeId);
        Assert.Null(block.EmployeeName);
        Assert.Equal("#FFA500", block.StatusColorCode); // Orange / Open slot
    }

    [Fact]
    public async Task Handle_UnpublishedAssignedShift_GetsBlueColorCode_AndMissingResourcesStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9);
        var startDateTime = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
        var endDateTime = startDateTime.AddHours(4);

        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 2, Title = "Clerk" };
        var employee = new Employee
        {
            Id = 12,
            FirstName = "Alice",
            LastName = "Smith",
            JobRoleId = 2,
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 20m }
        };

        var shift = new ShiftEntity
        {
            Id = 202,
            SiteId = 1,
            JobRoleId = 2,
            JobRole = role,
            EmployeeId = 12,
            StartTime = startDateTime,
            EndTime = endDateTime,
            IsPublished = false // Unpublished
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = new GetDailyShiftScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetDailyShiftScheduleQuery(1, targetDate), CancellationToken.None);

        // Assert
        var targetBadge = result.WeekCalendarStrip.First(b => b.Date == targetDate);
        Assert.Equal(WeekDayCalendarStatus.MissingResourcesOrUnpublished, targetBadge.Status);

        var interval = result.HourlyTimeline.First(i => i.StartHour == new TimeOnly(9, 0));
        var block = Assert.Single(interval.Blocks);
        Assert.Equal("#3B82F6", block.StatusColorCode); // Blue / Unpublished draft
    }

    [Fact]
    public async Task Handle_MisallocatedJobRole_GetsRedColorCode_AndOvertimeOrMisallocationStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9);
        var startDateTime = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
        var endDateTime = startDateTime.AddHours(4);

        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var shiftRole = new JobRole { Id = 1, Title = "Manager" };
        var empRole = new JobRole { Id = 2, Title = "Clerk" };

        var employee = new Employee
        {
            Id = 15,
            FirstName = "Bob",
            LastName = "Jones",
            JobRoleId = 2, // Clerk, but shift requires Manager (1)
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 30m }
        };

        var shift = new ShiftEntity
        {
            Id = 203,
            SiteId = 1,
            JobRoleId = 1,
            JobRole = shiftRole,
            EmployeeId = 15,
            StartTime = startDateTime,
            EndTime = endDateTime,
            IsPublished = true
        };

        context.Sites.Add(site);
        context.JobRoles.AddRange(shiftRole, empRole);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = new GetDailyShiftScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetDailyShiftScheduleQuery(1, targetDate), CancellationToken.None);

        // Assert
        var targetBadge = result.WeekCalendarStrip.First(b => b.Date == targetDate);
        Assert.Equal(WeekDayCalendarStatus.OvertimeOrMisallocation, targetBadge.Status);

        var interval = result.HourlyTimeline.First(i => i.StartHour == new TimeOnly(9, 0));
        var block = Assert.Single(interval.Blocks);
        Assert.Equal("#EF4444", block.StatusColorCode); // Red / Misallocation
    }

    [Fact]
    public async Task Handle_ShiftOver8Hours_ReturnsOvertimeOrMisallocationStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9);
        var startDateTime = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
        var endDateTime = startDateTime.AddHours(9); // 9 hours > 8 hours

        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 1, Title = "Staff" };
        var employee = new Employee
        {
            Id = 16,
            FirstName = "Sam",
            LastName = "Taylor",
            JobRoleId = 1,
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 20m }
        };

        var shift = new ShiftEntity
        {
            Id = 204,
            SiteId = 1,
            JobRoleId = 1,
            JobRole = role,
            EmployeeId = 16,
            StartTime = startDateTime,
            EndTime = endDateTime,
            IsPublished = true
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = new GetDailyShiftScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetDailyShiftScheduleQuery(1, targetDate), CancellationToken.None);

        // Assert
        var targetBadge = result.WeekCalendarStrip.First(b => b.Date == targetDate);
        Assert.Equal(WeekDayCalendarStatus.OvertimeOrMisallocation, targetBadge.Status);
    }

    [Fact]
    public async Task Handle_SiteDayOff_ReturnsDimmedDayOff()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 11); // Friday

        var site = new Site
        {
            Id = 1,
            SiteName = "Main Branch",
            OperationalHours = new List<SiteOperationalHour>
            {
                new() { DayOfWeek = DayOfWeek.Friday, IsOpen = false }
            }
        };

        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var handler = new GetDailyShiftScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetDailyShiftScheduleQuery(1, targetDate), CancellationToken.None);

        // Assert
        Assert.True(result.IsDayOff);
        var targetBadge = result.WeekCalendarStrip.First(b => b.Date == targetDate);
        Assert.True(targetBadge.IsDayOff);
        Assert.Equal(WeekDayCalendarStatus.DimmedDayOff, targetBadge.Status);
    }

    [Fact]
    public async Task Handle_MonthlySalary_CalculatesHourlyRateFrom160HoursDivisor()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9);
        var startDateTime = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
        var endDateTime = startDateTime.AddHours(8);

        var site = new Site { Id = 1, SiteName = "Main Branch" };
        var role = new JobRole { Id = 1, Title = "Specialist" };
        var employee = new Employee
        {
            Id = 20,
            FirstName = "Mona",
            LastName = "Ali",
            JobRoleId = 1,
            PayrollProfile = new PayrollProfile
            {
                SalaryType = "Monthly",
                PaymentAmount = 3200m // 3200 / 160 = 20.00/hr
            }
        };

        var shift = new ShiftEntity
        {
            Id = 301,
            SiteId = 1,
            JobRoleId = 1,
            JobRole = role,
            EmployeeId = 20,
            StartTime = startDateTime,
            EndTime = endDateTime,
            IsPublished = true
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = new GetDailyShiftScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act
        var result = await handler.Handle(new GetDailyShiftScheduleQuery(1, targetDate), CancellationToken.None);

        // Assert
        // 8 hours * 20.00 = 160.00
        Assert.Equal(160.00m, result.TotalEstimatedLaborCost);
    }

    [Fact]
    public async Task Handle_SiteNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = new GetDailyShiftScheduleQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new GetDailyShiftScheduleQuery(999, new DateOnly(2026, 9, 9)), CancellationToken.None));
    }
}
