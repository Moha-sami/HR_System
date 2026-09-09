using System.ComponentModel.DataAnnotations;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.CreateShiftBlock;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Schedules;

public class CreateShiftBlockTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static (CreateShiftBlockCommandHandler handler, UnitOfWork uow) CreateHandler(Buy2DbContext context)
    {
        var uow = new UnitOfWork(context);
        var handler = new CreateShiftBlockCommandHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<JobRole>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<SiteOperationalHour>(context),
            uow
        );
        return (handler, uow);
    }

    [Fact]
    public async Task Handle_DraftShiftBlockWithoutDispatch_PersistsAndReturnsDraftStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var site = new Site { Id = 1, SiteName = "HQ" };
        var jobRole = new JobRole { Id = 10, Title = "Cashier", IsActive = true, IsDeleted = false };
        var date = new DateOnly(2026, 9, 9); // Wednesday
        var opHour = new SiteOperationalHour
        {
            SiteId = 1,
            DayOfWeek = DayOfWeek.Wednesday,
            IsOpen = true,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(18, 0)
        };

        context.Sites.Add(site);
        context.JobRoles.Add(jobRole);
        context.SiteOperationalHours.Add(opHour);
        await context.SaveChangesAsync();

        var command = new CreateShiftBlockCommand(
            SiteId: 1,
            Date: date,
            StartTime: new TimeOnly(9, 0),
            EndTime: new TimeOnly(17, 0),
            JobRoleId: 10,
            DispatchPolicy: ShiftMarketDispatchPolicy.None
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.ShiftId > 0);
        Assert.Equal(1, result.SiteId);
        Assert.Equal(10, result.JobRoleId);
        Assert.Equal("Cashier", result.RoleTitle);
        Assert.Equal(new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero), result.StartTime);
        Assert.Equal(new DateTimeOffset(2026, 9, 9, 17, 0, 0, TimeSpan.Zero), result.EndTime);
        Assert.False(result.IsPublished);
        Assert.Null(result.EmployeeId);
        Assert.Equal("Draft", result.Status);
        Assert.Equal(ShiftMarketDispatchPolicy.None, result.DispatchPolicy);

        var savedShift = await context.ShiftEntities.FindAsync(result.ShiftId);
        Assert.NotNull(savedShift);
        Assert.False(savedShift.IsPublished);
        Assert.Null(savedShift.EmployeeId);
        Assert.Equal(1, savedShift.SiteId);
        Assert.Equal(10, savedShift.JobRoleId);
    }

    [Fact]
    public async Task Handle_FirstComeFirstServeDispatch_PersistsAndReturnsOpenInMarket()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var site = new Site { Id = 1, SiteName = "HQ" };
        var jobRole = new JobRole { Id = 10, Title = "Barista", IsActive = true, IsDeleted = false };
        var date = new DateOnly(2026, 9, 10); // Thursday
        var opHour = new SiteOperationalHour
        {
            SiteId = 1,
            DayOfWeek = DayOfWeek.Thursday,
            IsOpen = true,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(18, 0)
        };

        context.Sites.Add(site);
        context.JobRoles.Add(jobRole);
        context.SiteOperationalHours.Add(opHour);
        await context.SaveChangesAsync();

        var command = new CreateShiftBlockCommand(
            SiteId: 1,
            Date: date,
            StartTime: new TimeOnly(8, 30),
            EndTime: new TimeOnly(16, 30),
            JobRoleId: 10,
            DispatchPolicy: ShiftMarketDispatchPolicy.FirstComeFirstServe
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.ShiftId > 0);
        Assert.True(result.IsPublished);
        Assert.Null(result.EmployeeId);
        Assert.Equal("OpenInMarket", result.Status);
        Assert.Equal(ShiftMarketDispatchPolicy.FirstComeFirstServe, result.DispatchPolicy);

        var savedShift = await context.ShiftEntities.FindAsync(result.ShiftId);
        Assert.NotNull(savedShift);
        Assert.True(savedShift.IsPublished);
        Assert.Null(savedShift.EmployeeId);
    }

    [Fact]
    public async Task Handle_ClaimRequestDispatch_PersistsAndReturnsOpenInMarket()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var site = new Site { Id = 2, SiteName = "Branch A" };
        var jobRole = new JobRole { Id = 20, Title = "Supervisor", IsActive = true, IsDeleted = false };
        var date = new DateOnly(2026, 9, 11); // Friday
        var opHour = new SiteOperationalHour
        {
            SiteId = 2,
            DayOfWeek = DayOfWeek.Friday,
            IsOpen = true,
            OpenTime = new TimeOnly(7, 0),
            CloseTime = new TimeOnly(20, 0)
        };

        context.Sites.Add(site);
        context.JobRoles.Add(jobRole);
        context.SiteOperationalHours.Add(opHour);
        await context.SaveChangesAsync();

        var command = new CreateShiftBlockCommand(
            SiteId: 2,
            Date: date,
            StartTime: new TimeOnly(10, 0),
            EndTime: new TimeOnly(18, 0),
            JobRoleId: 20,
            DispatchPolicy: ShiftMarketDispatchPolicy.ClaimRequest
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.ShiftId > 0);
        Assert.True(result.IsPublished);
        Assert.Null(result.EmployeeId);
        Assert.Equal("OpenInMarket", result.Status);
        Assert.Equal(ShiftMarketDispatchPolicy.ClaimRequest, result.DispatchPolicy);

        var savedShift = await context.ShiftEntities.FindAsync(result.ShiftId);
        Assert.NotNull(savedShift);
        Assert.True(savedShift.IsPublished);
        Assert.Null(savedShift.EmployeeId);
    }

    [Theory]
    [InlineData("10:00", "09:00")]
    [InlineData("10:00", "10:00")]
    public async Task Handle_InvalidTimeRange_ThrowsValidationException(string startStr, string endStr)
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var command = new CreateShiftBlockCommand(
            SiteId: 1,
            Date: new DateOnly(2026, 9, 9),
            StartTime: TimeOnly.Parse(startStr),
            EndTime: TimeOnly.Parse(endStr),
            JobRoleId: 1
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Shift start time must be earlier than end time.", ex.Message);
    }

    [Fact]
    public async Task Handle_ClosedSiteOperationalHours_ThrowsValidationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var site = new Site { Id = 1, SiteName = "HQ" };
        var jobRole = new JobRole { Id = 10, Title = "Cashier", IsActive = true, IsDeleted = false };
        var date = new DateOnly(2026, 9, 13); // Sunday
        var opHour = new SiteOperationalHour
        {
            SiteId = 1,
            DayOfWeek = DayOfWeek.Sunday,
            IsOpen = false,
            OpenTime = new TimeOnly(0, 0),
            CloseTime = new TimeOnly(0, 0)
        };

        context.Sites.Add(site);
        context.JobRoles.Add(jobRole);
        context.SiteOperationalHours.Add(opHour);
        await context.SaveChangesAsync();

        var command = new CreateShiftBlockCommand(
            SiteId: 1,
            Date: date,
            StartTime: new TimeOnly(9, 0),
            EndTime: new TimeOnly(17, 0),
            JobRoleId: 10
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Site is closed on Sunday.", ex.Message);
    }

    [Theory]
    [InlineData("07:30", "17:00", "Shift time (07:30 - 17:00) falls outside operational hours (08:00 - 18:00).")]
    [InlineData("09:00", "18:30", "Shift time (09:00 - 18:30) falls outside operational hours (08:00 - 18:00).")]
    public async Task Handle_ShiftTimeExceedingOperationalHourWindow_ThrowsValidationException(
        string startStr,
        string endStr,
        string expectedMessage)
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var site = new Site { Id = 1, SiteName = "HQ" };
        var jobRole = new JobRole { Id = 10, Title = "Cashier", IsActive = true, IsDeleted = false };
        var date = new DateOnly(2026, 9, 9); // Wednesday
        var opHour = new SiteOperationalHour
        {
            SiteId = 1,
            DayOfWeek = DayOfWeek.Wednesday,
            IsOpen = true,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(18, 0)
        };

        context.Sites.Add(site);
        context.JobRoles.Add(jobRole);
        context.SiteOperationalHours.Add(opHour);
        await context.SaveChangesAsync();

        var command = new CreateShiftBlockCommand(
            SiteId: 1,
            Date: date,
            StartTime: TimeOnly.Parse(startStr),
            EndTime: TimeOnly.Parse(endStr),
            JobRoleId: 10
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public async Task Handle_NonExistentSite_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var command = new CreateShiftBlockCommand(
            SiteId: 999,
            Date: new DateOnly(2026, 9, 9),
            StartTime: new TimeOnly(9, 0),
            EndTime: new TimeOnly(17, 0),
            JobRoleId: 10
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Site with ID 999 not found.", ex.Message);
    }

    [Fact]
    public async Task Handle_NonExistentJobRole_ThrowsValidationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var site = new Site { Id = 1, SiteName = "HQ" };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var command = new CreateShiftBlockCommand(
            SiteId: 1,
            Date: new DateOnly(2026, 9, 9),
            StartTime: new TimeOnly(9, 0),
            EndTime: new TimeOnly(17, 0),
            JobRoleId: 999
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Job role with ID 999 is not active or does not exist.", ex.Message);
    }

    [Fact]
    public async Task Handle_InactiveJobRole_ThrowsValidationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var site = new Site { Id = 1, SiteName = "HQ" };
        var jobRole = new JobRole { Id = 10, Title = "Cashier", IsActive = false, IsDeleted = false };
        context.Sites.Add(site);
        context.JobRoles.Add(jobRole);
        await context.SaveChangesAsync();

        var command = new CreateShiftBlockCommand(
            SiteId: 1,
            Date: new DateOnly(2026, 9, 9),
            StartTime: new TimeOnly(9, 0),
            EndTime: new TimeOnly(17, 0),
            JobRoleId: 10
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Job role with ID 10 is not active or does not exist.", ex.Message);
    }

    [Fact]
    public async Task Handle_DeletedJobRole_ThrowsValidationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var site = new Site { Id = 1, SiteName = "HQ" };
        var jobRole = new JobRole { Id = 10, Title = "Cashier", IsActive = true, IsDeleted = true };
        context.Sites.Add(site);
        context.JobRoles.Add(jobRole);
        await context.SaveChangesAsync();

        var command = new CreateShiftBlockCommand(
            SiteId: 1,
            Date: new DateOnly(2026, 9, 9),
            StartTime: new TimeOnly(9, 0),
            EndTime: new TimeOnly(17, 0),
            JobRoleId: 10
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("Job role with ID 10 is not active or does not exist.", ex.Message);
    }
}
