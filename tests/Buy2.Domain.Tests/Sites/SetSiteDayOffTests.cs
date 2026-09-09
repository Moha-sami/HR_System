using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Sites.SetSiteDayOff;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Sites;

public class SetSiteDayOffTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static (SetSiteDayOffCommandHandler handler, UnitOfWork uow) CreateHandler(Buy2DbContext context)
    {
        var uow = new UnitOfWork(context);
        var handler = new SetSiteDayOffCommandHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<SiteOperationalHour>(context),
            new GenericRepository<ShiftEntity>(context),
            uow
        );
        return (handler, uow);
    }

    [Fact]
    public async Task Handle_ToggleDayOffToTrue_UpdatesOperationalHourToClosedAndReturnsDimmedDayOff()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var site = new Site { Id = 1, SiteName = "Test Branch" };
        var opHour = new SiteOperationalHour
        {
            SiteId = 1,
            DayOfWeek = DayOfWeek.Monday,
            IsOpen = true,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(18, 0)
        };
        context.Sites.Add(site);
        context.SiteOperationalHours.Add(opHour);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var mondayDate = new DateOnly(2026, 9, 7); // Monday
        var command = new SetSiteDayOffCommand(1, mondayDate, IsDayOff: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.SiteId);
        Assert.Equal("Test Branch", result.SiteName);
        Assert.Equal(mondayDate, result.Date);
        Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
        Assert.True(result.IsDayOff);
        Assert.Equal(WeekDayCalendarStatus.DimmedDayOff, result.CoverageStatus);
        Assert.Equal(0, result.ExistingShiftsCount);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));

        var updatedOpHour = await context.SiteOperationalHours
            .FirstOrDefaultAsync(o => o.SiteId == 1 && o.DayOfWeek == DayOfWeek.Monday);
        Assert.NotNull(updatedOpHour);
        Assert.False(updatedOpHour.IsOpen);
    }

    [Fact]
    public async Task Handle_ToggleDayOffToFalse_UpdatesOperationalHourToOpenAndReturnsOperationalStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var site = new Site { Id = 2, SiteName = "Cairo Branch" };
        var opHour = new SiteOperationalHour
        {
            SiteId = 2,
            DayOfWeek = DayOfWeek.Tuesday,
            IsOpen = false,
            OpenTime = new TimeOnly(9, 0),
            CloseTime = new TimeOnly(17, 0)
        };
        context.Sites.Add(site);
        context.SiteOperationalHours.Add(opHour);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var tuesdayDate = new DateOnly(2026, 9, 8); // Tuesday
        var command = new SetSiteDayOffCommand(2, tuesdayDate, IsDayOff: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.SiteId);
        Assert.Equal("Cairo Branch", result.SiteName);
        Assert.Equal(tuesdayDate, result.Date);
        Assert.Equal(DayOfWeek.Tuesday, result.DayOfWeek);
        Assert.False(result.IsDayOff);
        Assert.Equal(WeekDayCalendarStatus.NoAllocations, result.CoverageStatus);
        Assert.Equal(0, result.ExistingShiftsCount);

        var updatedOpHour = await context.SiteOperationalHours
            .FirstOrDefaultAsync(o => o.SiteId == 2 && o.DayOfWeek == DayOfWeek.Tuesday);
        Assert.NotNull(updatedOpHour);
        Assert.True(updatedOpHour.IsOpen);
    }

    [Fact]
    public async Task Handle_NoOperationalHourExisted_CreatesDefaultOperationalHourRecord()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var site = new Site { Id = 3, SiteName = "Alex Branch" };
        context.Sites.Add(site);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var wednesdayDate = new DateOnly(2026, 9, 9); // Wednesday
        var command = new SetSiteDayOffCommand(3, wednesdayDate, IsDayOff: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsDayOff);
        var createdOpHour = await context.SiteOperationalHours
            .FirstOrDefaultAsync(o => o.SiteId == 3 && o.DayOfWeek == DayOfWeek.Wednesday);
        Assert.NotNull(createdOpHour);
        Assert.Equal(3, createdOpHour.SiteId);
        Assert.Equal(DayOfWeek.Wednesday, createdOpHour.DayOfWeek);
        Assert.False(createdOpHour.IsOpen);
        Assert.Equal(new TimeOnly(9, 0), createdOpHour.OpenTime);
        Assert.Equal(new TimeOnly(17, 0), createdOpHour.CloseTime);
    }

    [Fact]
    public async Task Handle_PreservesExistingShiftsAndReportsCount_WhenMarkedAsDayOff()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var site = new Site { Id = 4, SiteName = "Giza Hub" };
        var targetDate = new DateOnly(2026, 9, 10); // Thursday
        var startOffset = new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);

        var shift1 = new ShiftEntity
        {
            Id = 101,
            SiteId = 4,
            JobRoleId = 1,
            StartTime = startOffset,
            EndTime = startOffset.AddHours(8),
            IsPublished = true,
            EmployeeId = 55
        };
        var shift2 = new ShiftEntity
        {
            Id = 102,
            SiteId = 4,
            JobRoleId = 1,
            StartTime = startOffset.AddHours(4),
            EndTime = startOffset.AddHours(12),
            IsPublished = false,
            EmployeeId = null
        };
        var otherDateShift = new ShiftEntity
        {
            Id = 103,
            SiteId = 4,
            JobRoleId = 1,
            StartTime = startOffset.AddDays(1),
            EndTime = startOffset.AddDays(1).AddHours(8),
            IsPublished = true,
            EmployeeId = 55
        };

        context.Sites.Add(site);
        context.ShiftEntities.AddRange(shift1, shift2, otherDateShift);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var command = new SetSiteDayOffCommand(4, targetDate, IsDayOff: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsDayOff);
        Assert.Equal(WeekDayCalendarStatus.DimmedDayOff, result.CoverageStatus);
        Assert.Equal(2, result.ExistingShiftsCount);

        // Verify shifts are preserved in database
        var remainingShifts = await context.ShiftEntities.Where(s => s.SiteId == 4).ToListAsync();
        Assert.Equal(3, remainingShifts.Count);
    }

    [Fact]
    public async Task Handle_ToggledToWorkingDay_WithAssignedAndPublishedShifts_ReturnsCoveredAndPublished()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var site = new Site { Id = 5, SiteName = "Nasr City Hub" };
        var targetDate = new DateOnly(2026, 9, 11); // Friday
        var startOffset = new DateTimeOffset(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);

        var shift = new ShiftEntity
        {
            Id = 201,
            SiteId = 5,
            JobRoleId = 1,
            StartTime = startOffset,
            EndTime = startOffset.AddHours(8),
            IsPublished = true,
            EmployeeId = 12
        };

        context.Sites.Add(site);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var command = new SetSiteDayOffCommand(5, targetDate, IsDayOff: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsDayOff);
        Assert.Equal(WeekDayCalendarStatus.CoveredAndPublished, result.CoverageStatus);
        Assert.Equal(1, result.ExistingShiftsCount);
    }

    [Fact]
    public async Task Handle_ToggledToWorkingDay_WithUnassignedOrUnpublishedShifts_ReturnsMissingResources()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var site = new Site { Id = 6, SiteName = "Zayed Hub" };
        var targetDate = new DateOnly(2026, 9, 12); // Saturday
        var startOffset = new DateTimeOffset(2026, 9, 12, 9, 0, 0, TimeSpan.Zero);

        var shift = new ShiftEntity
        {
            Id = 301,
            SiteId = 6,
            JobRoleId = 1,
            StartTime = startOffset,
            EndTime = startOffset.AddHours(8),
            IsPublished = false, // Unpublished
            EmployeeId = null   // Unassigned
        };

        context.Sites.Add(site);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var command = new SetSiteDayOffCommand(6, targetDate, IsDayOff: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsDayOff);
        Assert.Equal(WeekDayCalendarStatus.MissingResourcesOrUnpublished, result.CoverageStatus);
        Assert.Equal(1, result.ExistingShiftsCount);
    }

    [Fact]
    public async Task Handle_NonExistentSite_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var command = new SetSiteDayOffCommand(999, new DateOnly(2026, 9, 7), IsDayOff: true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Site with ID 999 not found", ex.Message);
    }
}
