using System.ComponentModel.DataAnnotations;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.UnassignOrDeleteShiftBlock;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Schedules;

public class UnassignOrDeleteShiftBlockTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static (UnassignOrDeleteShiftBlockCommandHandler handler, UnitOfWork uow) CreateHandler(
        Buy2DbContext context,
        bool includeOperationalHoursRepo = true)
    {
        var uow = new UnitOfWork(context);
        var handler = new UnassignOrDeleteShiftBlockCommandHandler(
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context),
            new GenericRepository<Site>(context),
            uow,
            includeOperationalHoursRepo ? new GenericRepository<SiteOperationalHour>(context) : null
        );
        return (handler, uow);
    }

    [Fact]
    public async Task Handle_UnassignEmployeePublishedShift_RevertsToOpenStateWithOrangeStatusColor()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cashier" };
        var site = new Site { Id = 10, SiteName = "Downtown Store" };
        var opHour = new SiteOperationalHour
        {
            SiteId = 10,
            DayOfWeek = DayOfWeek.Wednesday,
            IsOpen = true,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(20, 0)
        };

        var employee = new Employee
        {
            Id = 100,
            FirstName = "Alice",
            LastName = "Smith",
            JobRoleId = 1,
            JobRole = role,
            SiteId = 10,
            IsActive = true,
            IsDeleted = false,
            PayrollProfile = new PayrollProfile
            {
                SalaryType = "Hourly",
                PaymentAmount = 25.00m
            }
        };

        var shiftDate = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero); // Wednesday
        var shift = new ShiftEntity
        {
            Id = 50,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = role,
            StartTime = shiftDate,
            EndTime = shiftDate.AddHours(8),
            IsPublished = true,
            EmployeeId = 100
        };

        context.JobRoles.Add(role);
        context.Sites.Add(site);
        context.SiteOperationalHours.Add(opHour);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var command = new UnassignOrDeleteShiftBlockCommand(
            ShiftBlockId: 50,
            Action: ShiftBlockRemovalAction.UnassignOnly
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsDeleted);
        Assert.Equal(ShiftBlockRemovalAction.UnassignOnly, result.ActionTaken);
        Assert.NotNull(result.UpdatedBlock);
        Assert.Null(result.UpdatedBlock.EmployeeId);
        Assert.Null(result.UpdatedBlock.EmployeeName);
        Assert.Null(result.UpdatedBlock.EmployeeAvatarUrl);
        Assert.Equal("#FFA500", result.UpdatedBlock.StatusColorCode); // Orange / Published open slot
        Assert.Equal(0m, result.UpdatedDailyLaborCost);
        Assert.Equal(WeekDayCalendarStatus.MissingResourcesOrUnpublished, result.SiteCoverageStatus);

        var persistedShift = await context.ShiftEntities.FindAsync(50);
        Assert.NotNull(persistedShift);
        Assert.Null(persistedShift.EmployeeId);
    }

    [Fact]
    public async Task Handle_UnassignEmployeeDraftShift_RevertsToOpenStateWithBlueStatusColor()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cashier" };
        var site = new Site { Id = 10, SiteName = "Downtown Store" };
        var opHour = new SiteOperationalHour
        {
            SiteId = 10,
            DayOfWeek = DayOfWeek.Wednesday,
            IsOpen = true,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(20, 0)
        };

        var employee = new Employee
        {
            Id = 101,
            FirstName = "Bob",
            LastName = "Jones",
            JobRoleId = 1,
            JobRole = role,
            SiteId = 10,
            IsActive = true,
            IsDeleted = false,
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 20.00m }
        };

        var shiftDate = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero);
        var shift = new ShiftEntity
        {
            Id = 51,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = role,
            StartTime = shiftDate,
            EndTime = shiftDate.AddHours(6),
            IsPublished = false, // Draft
            EmployeeId = 101
        };

        context.JobRoles.Add(role);
        context.Sites.Add(site);
        context.SiteOperationalHours.Add(opHour);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var command = new UnassignOrDeleteShiftBlockCommand(
            ShiftBlockId: 51,
            Action: ShiftBlockRemovalAction.UnassignOnly
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsDeleted);
        Assert.Equal(ShiftBlockRemovalAction.UnassignOnly, result.ActionTaken);
        Assert.NotNull(result.UpdatedBlock);
        Assert.Null(result.UpdatedBlock.EmployeeId);
        Assert.Equal("#3B82F6", result.UpdatedBlock.StatusColorCode); // Blue / Draft open slot
        Assert.Equal(0m, result.UpdatedDailyLaborCost);
        Assert.Equal(WeekDayCalendarStatus.MissingResourcesOrUnpublished, result.SiteCoverageStatus);
    }

    [Fact]
    public async Task Handle_UnassignAlreadyUnassignedShift_IsNoOpAndReturnsValidResponse()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cashier" };
        var site = new Site { Id = 10, SiteName = "Downtown Store" };
        var opHour = new SiteOperationalHour
        {
            SiteId = 10,
            DayOfWeek = DayOfWeek.Wednesday,
            IsOpen = true,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(20, 0)
        };

        var shiftDate = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero);
        var shift = new ShiftEntity
        {
            Id = 52,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = role,
            StartTime = shiftDate,
            EndTime = shiftDate.AddHours(5),
            IsPublished = true,
            EmployeeId = null // Already unassigned
        };

        context.JobRoles.Add(role);
        context.Sites.Add(site);
        context.SiteOperationalHours.Add(opHour);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var command = new UnassignOrDeleteShiftBlockCommand(
            ShiftBlockId: 52,
            Action: ShiftBlockRemovalAction.UnassignOnly
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsDeleted);
        Assert.Equal(ShiftBlockRemovalAction.UnassignOnly, result.ActionTaken);
        Assert.NotNull(result.UpdatedBlock);
        Assert.Null(result.UpdatedBlock.EmployeeId);
        Assert.Equal("#FFA500", result.UpdatedBlock.StatusColorCode);
    }

    [Fact]
    public async Task Handle_DeleteUnassignedDraftShift_HardDeletesEntityAndReturnsIsDeletedTrue()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cashier" };
        var site = new Site { Id = 10, SiteName = "Downtown Store" };
        var opHour = new SiteOperationalHour
        {
            SiteId = 10,
            DayOfWeek = DayOfWeek.Wednesday,
            IsOpen = true,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(20, 0)
        };

        var shiftDate = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero);
        var shift = new ShiftEntity
        {
            Id = 53,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = role,
            StartTime = shiftDate,
            EndTime = shiftDate.AddHours(8),
            IsPublished = false, // Draft
            EmployeeId = null
        };

        context.JobRoles.Add(role);
        context.Sites.Add(site);
        context.SiteOperationalHours.Add(opHour);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var command = new UnassignOrDeleteShiftBlockCommand(
            ShiftBlockId: 53,
            Action: ShiftBlockRemovalAction.DeleteBlock,
            ConfirmPublishedDeletion: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsDeleted);
        Assert.Equal(ShiftBlockRemovalAction.DeleteBlock, result.ActionTaken);
        Assert.Null(result.UpdatedBlock);
        Assert.Equal(0m, result.UpdatedDailyLaborCost);
        Assert.Equal(WeekDayCalendarStatus.NoAllocations, result.SiteCoverageStatus);

        var persisted = await context.ShiftEntities.FindAsync(53);
        Assert.Null(persisted);
    }

    [Fact]
    public async Task Handle_DeletePublishedShiftWithoutConfirmation_ThrowsValidationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cashier" };
        var site = new Site { Id = 10, SiteName = "Downtown Store" };
        var shiftDate = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero);
        var shift = new ShiftEntity
        {
            Id = 54,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = role,
            StartTime = shiftDate,
            EndTime = shiftDate.AddHours(8),
            IsPublished = true, // Published
            EmployeeId = null
        };

        context.JobRoles.Add(role);
        context.Sites.Add(site);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var command = new UnassignOrDeleteShiftBlockCommand(
            ShiftBlockId: 54,
            Action: ShiftBlockRemovalAction.DeleteBlock,
            ConfirmPublishedDeletion: false
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Cannot delete a published shift block without explicit confirmation", ex.Message);

        var persisted = await context.ShiftEntities.FindAsync(54);
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task Handle_DeletePublishedShiftWithConfirmPublishedDeletionTrue_SucceedsAndDeletesEntity()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cashier" };
        var site = new Site { Id = 10, SiteName = "Downtown Store" };
        var opHour = new SiteOperationalHour
        {
            SiteId = 10,
            DayOfWeek = DayOfWeek.Wednesday,
            IsOpen = true,
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(20, 0)
        };

        var shiftDate = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero);
        var shift = new ShiftEntity
        {
            Id = 55,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = role,
            StartTime = shiftDate,
            EndTime = shiftDate.AddHours(8),
            IsPublished = true,
            EmployeeId = null
        };

        context.JobRoles.Add(role);
        context.Sites.Add(site);
        context.SiteOperationalHours.Add(opHour);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var command = new UnassignOrDeleteShiftBlockCommand(
            ShiftBlockId: 55,
            Action: ShiftBlockRemovalAction.DeleteBlock,
            ConfirmPublishedDeletion: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsDeleted);
        Assert.Equal(ShiftBlockRemovalAction.DeleteBlock, result.ActionTaken);
        Assert.Null(result.UpdatedBlock);
        Assert.Equal(WeekDayCalendarStatus.NoAllocations, result.SiteCoverageStatus);

        var persisted = await context.ShiftEntities.FindAsync(55);
        Assert.Null(persisted);
    }

    [Fact]
    public async Task Handle_NonExistentShiftBlockId_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var command = new UnassignOrDeleteShiftBlockCommand(
            ShiftBlockId: 9999,
            Action: ShiftBlockRemovalAction.UnassignOnly
        );

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RecalculatesLaborCostAndCoverageStatusAccurately_AfterRemoval()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cook" };
        var site = new Site { Id = 20, SiteName = "Uptown Diner" };
        var opHour = new SiteOperationalHour
        {
            SiteId = 20,
            DayOfWeek = DayOfWeek.Wednesday,
            IsOpen = true,
            OpenTime = new TimeOnly(7, 0),
            CloseTime = new TimeOnly(22, 0)
        };

        var emp1 = new Employee
        {
            Id = 201,
            FirstName = "Chef",
            LastName = "One",
            JobRoleId = 1,
            JobRole = role,
            SiteId = 20,
            IsActive = true,
            IsDeleted = false,
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 30m }
        };

        var emp2 = new Employee
        {
            Id = 202,
            FirstName = "Chef",
            LastName = "Two",
            JobRoleId = 1,
            JobRole = role,
            SiteId = 20,
            IsActive = true,
            IsDeleted = false,
            PayrollProfile = new PayrollProfile { SalaryType = "Monthly", PaymentAmount = 3200m } // 3200/160 = $20/h
        };

        var targetDate = new DateTimeOffset(2026, 9, 9, 8, 0, 0, TimeSpan.Zero);

        // Shift 1: 5 hours * $30 = $150 (emp1 assigned, published)
        var shift1 = new ShiftEntity
        {
            Id = 61,
            SiteId = 20,
            JobRoleId = 1,
            JobRole = role,
            EmployeeId = 201,
            StartTime = targetDate,
            EndTime = targetDate.AddHours(5),
            IsPublished = true
        };

        // Shift 2: 4 hours * $20 = $80 (emp2 assigned, published) -> To be unassigned!
        var shift2 = new ShiftEntity
        {
            Id = 62,
            SiteId = 20,
            JobRoleId = 1,
            JobRole = role,
            EmployeeId = 202,
            StartTime = targetDate.AddHours(5),
            EndTime = targetDate.AddHours(9),
            IsPublished = true
        };

        context.JobRoles.Add(role);
        context.Sites.Add(site);
        context.SiteOperationalHours.Add(opHour);
        context.Employees.AddRange(emp1, emp2);
        context.ShiftEntities.AddRange(shift1, shift2);
        await context.SaveChangesAsync();

        var command = new UnassignOrDeleteShiftBlockCommand(
            ShiftBlockId: 62,
            Action: ShiftBlockRemovalAction.UnassignOnly
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        // Labor cost should now only include shift 1 (5h * $30 = $150.00m)
        Assert.Equal(150.00m, result.UpdatedDailyLaborCost);
        // Coverage status should be MissingResourcesOrUnpublished because shift 2 is now open
        Assert.Equal(WeekDayCalendarStatus.MissingResourcesOrUnpublished, result.SiteCoverageStatus);
        Assert.NotNull(result.UpdatedBlock);
        Assert.Equal("#FFA500", result.UpdatedBlock.StatusColorCode); // published open slot
    }

    [Fact]
    public async Task Handle_DeleteShift_WhenRemainingShiftsAreAllAssignedAndPublished_ReturnsCoveredAndPublished()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cook" };
        var site = new Site { Id = 20, SiteName = "Uptown Diner" };
        var opHour = new SiteOperationalHour
        {
            SiteId = 20,
            DayOfWeek = DayOfWeek.Wednesday,
            IsOpen = true,
            OpenTime = new TimeOnly(7, 0),
            CloseTime = new TimeOnly(22, 0)
        };

        var emp = new Employee
        {
            Id = 301,
            FirstName = "Chef",
            LastName = "A",
            JobRoleId = 1,
            JobRole = role,
            SiteId = 20,
            IsActive = true,
            IsDeleted = false,
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 25m }
        };

        var targetDate = new DateTimeOffset(2026, 9, 9, 8, 0, 0, TimeSpan.Zero);

        // Fully covered shift 1
        var shift1 = new ShiftEntity
        {
            Id = 71,
            SiteId = 20,
            JobRoleId = 1,
            JobRole = role,
            EmployeeId = 301,
            StartTime = targetDate,
            EndTime = targetDate.AddHours(7),
            IsPublished = true
        };

        // Extra draft unassigned shift 2 to be deleted
        var shift2 = new ShiftEntity
        {
            Id = 72,
            SiteId = 20,
            JobRoleId = 1,
            JobRole = role,
            EmployeeId = null,
            StartTime = targetDate.AddHours(7),
            EndTime = targetDate.AddHours(10),
            IsPublished = false
        };

        context.JobRoles.Add(role);
        context.Sites.Add(site);
        context.SiteOperationalHours.Add(opHour);
        context.Employees.Add(emp);
        context.ShiftEntities.AddRange(shift1, shift2);
        await context.SaveChangesAsync();

        var command = new UnassignOrDeleteShiftBlockCommand(
            ShiftBlockId: 72,
            Action: ShiftBlockRemovalAction.DeleteBlock
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsDeleted);
        Assert.Equal(175.00m, result.UpdatedDailyLaborCost); // 7h * $25 = 175
        Assert.Equal(WeekDayCalendarStatus.CoveredAndPublished, result.SiteCoverageStatus);
    }

    [Fact]
    public async Task Handle_SiteClosedOnTargetDate_ReturnsDimmedDayOffCoverageStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cook" };
        var site = new Site { Id = 30, SiteName = "Sunday Closed Shop" };
        var opHour = new SiteOperationalHour
        {
            SiteId = 30,
            DayOfWeek = DayOfWeek.Sunday,
            IsOpen = false // Closed!
        };

        var sundayDate = new DateTimeOffset(2026, 9, 13, 9, 0, 0, TimeSpan.Zero); // Sunday
        var shift = new ShiftEntity
        {
            Id = 81,
            SiteId = 30,
            JobRoleId = 1,
            JobRole = role,
            EmployeeId = null,
            StartTime = sundayDate,
            EndTime = sundayDate.AddHours(4),
            IsPublished = false
        };

        context.JobRoles.Add(role);
        context.Sites.Add(site);
        context.SiteOperationalHours.Add(opHour);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var command = new UnassignOrDeleteShiftBlockCommand(
            ShiftBlockId: 81,
            Action: ShiftBlockRemovalAction.DeleteBlock
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsDeleted);
        Assert.Equal(WeekDayCalendarStatus.DimmedDayOff, result.SiteCoverageStatus);
    }

    [Fact]
    public async Task Handle_OvertimeOnRemainingShift_ReturnsOvertimeOrMisallocationCoverageStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cashier" };
        var site = new Site { Id = 10, SiteName = "Downtown Store" };
        var opHour = new SiteOperationalHour
        {
            SiteId = 10,
            DayOfWeek = DayOfWeek.Wednesday,
            IsOpen = true,
            OpenTime = new TimeOnly(6, 0),
            CloseTime = new TimeOnly(23, 0)
        };

        var emp = new Employee
        {
            Id = 401,
            FirstName = "Overtime",
            LastName = "Worker",
            JobRoleId = 1,
            JobRole = role,
            SiteId = 10,
            IsActive = true,
            IsDeleted = false,
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 15m }
        };

        var targetDate = new DateTimeOffset(2026, 9, 9, 8, 0, 0, TimeSpan.Zero);

        // Remaining shift has 9 hours (> 8 hours overtime)
        var shiftRemaining = new ShiftEntity
        {
            Id = 91,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = role,
            EmployeeId = 401,
            StartTime = targetDate,
            EndTime = targetDate.AddHours(9),
            IsPublished = true
        };

        var shiftToDelete = new ShiftEntity
        {
            Id = 92,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = role,
            EmployeeId = null,
            StartTime = targetDate.AddHours(10),
            EndTime = targetDate.AddHours(14),
            IsPublished = false
        };

        context.JobRoles.Add(role);
        context.Sites.Add(site);
        context.SiteOperationalHours.Add(opHour);
        context.Employees.Add(emp);
        context.ShiftEntities.AddRange(shiftRemaining, shiftToDelete);
        await context.SaveChangesAsync();

        var command = new UnassignOrDeleteShiftBlockCommand(
            ShiftBlockId: 92,
            Action: ShiftBlockRemovalAction.DeleteBlock
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsDeleted);
        Assert.Equal(WeekDayCalendarStatus.OvertimeOrMisallocation, result.SiteCoverageStatus);
    }
}
