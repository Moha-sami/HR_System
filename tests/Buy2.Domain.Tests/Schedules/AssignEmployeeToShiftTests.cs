using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.AssignEmployeeToShift;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Schedules;

public class AssignEmployeeToShiftTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static (AssignEmployeeToShiftCommandHandler handler, UnitOfWork uow) CreateHandler(Buy2DbContext context)
    {
        var uow = new UnitOfWork(context);
        var handler = new AssignEmployeeToShiftCommandHandler(
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context),
            uow
        );
        return (handler, uow);
    }

    [Fact]
    public async Task Handle_CleanCompliantAssignment_CommitsAndReturnsSuccessWithGreenStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cashier" };
        var site = new Site { Id = 10, SiteName = "Store A" };
        var employee = new Employee
        {
            Id = 100,
            FirstName = "John",
            LastName = "Doe",
            JobRoleId = 1,
            JobRole = role,
            SiteId = 10,
            IsActive = true,
            IsDeleted = false,
            PayrollProfile = new PayrollProfile
            {
                SalaryType = "Hourly",
                PaymentAmount = 20.00m
            }
        };

        var shiftDate = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero);
        var shift = new ShiftEntity
        {
            Id = 50,
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
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var command = new AssignEmployeeToShiftCommand(
            ShiftBlockId: 50,
            EmployeeId: 100,
            ConfirmOverride: false,
            OverrideReason: null
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.False(result.HasConflicts);
        Assert.Empty(result.Warnings);
        Assert.False(result.WasOverridden);
        Assert.Null(result.OverrideReason);
        Assert.NotNull(result.UpdatedBlock);
        Assert.Equal(100, result.UpdatedBlock.EmployeeId);
        Assert.Equal("John Doe", result.UpdatedBlock.EmployeeName);
        Assert.Equal("#10B981", result.UpdatedBlock.StatusColorCode); // Green / Compliant
        Assert.Equal(160.00m, result.UpdatedDailyLaborCost); // 8h * $20

        var persisted = await context.ShiftEntities.FindAsync(50);
        Assert.NotNull(persisted);
        Assert.Equal(100, persisted.EmployeeId);
    }

    [Fact]
    public async Task Handle_QualificationMismatchWithoutOverride_ReturnsFailureAndDoesNotCommit()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var cashierRole = new JobRole { Id = 1, Title = "Cashier" };
        var securityRole = new JobRole { Id = 2, Title = "Security" };
        var employee = new Employee
        {
            Id = 101,
            FirstName = "Alice",
            LastName = "Smith",
            JobRoleId = 2,
            JobRole = securityRole,
            IsActive = true,
            IsDeleted = false
        };

        var shiftDate = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero);
        var shift = new ShiftEntity
        {
            Id = 51,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = cashierRole,
            StartTime = shiftDate,
            EndTime = shiftDate.AddHours(7),
            IsPublished = true,
            EmployeeId = null
        };

        context.JobRoles.AddRange(cashierRole, securityRole);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var command = new AssignEmployeeToShiftCommand(
            ShiftBlockId: 51,
            EmployeeId: 101,
            ConfirmOverride: false,
            OverrideReason: null
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.HasConflicts);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(AssignmentConflictType.QualificationMismatch, warning.ConflictType);
        Assert.Contains("Security", warning.Message);
        Assert.Contains("Cashier", warning.Message);
        Assert.False(result.WasOverridden);
        Assert.Null(result.UpdatedBlock);

        var persisted = await context.ShiftEntities.FindAsync(51);
        Assert.NotNull(persisted);
        Assert.Null(persisted.EmployeeId);
    }

    [Fact]
    public async Task Handle_QualificationMismatchWithOverride_CommitsAndReturnsRedStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var cashierRole = new JobRole { Id = 1, Title = "Cashier" };
        var securityRole = new JobRole { Id = 2, Title = "Security" };
        var employee = new Employee
        {
            Id = 102,
            FirstName = "Bob",
            LastName = "Taylor",
            JobRoleId = 2,
            JobRole = securityRole,
            IsActive = true,
            IsDeleted = false,
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 15m }
        };

        var shiftDate = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero);
        var shift = new ShiftEntity
        {
            Id = 52,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = cashierRole,
            StartTime = shiftDate,
            EndTime = shiftDate.AddHours(6),
            IsPublished = true,
            EmployeeId = null
        };

        context.JobRoles.AddRange(cashierRole, securityRole);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var command = new AssignEmployeeToShiftCommand(
            ShiftBlockId: 52,
            EmployeeId: 102,
            ConfirmOverride: true,
            OverrideReason: "Urgent coverage needed"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.HasConflicts);
        Assert.True(result.WasOverridden);
        Assert.Equal("Urgent coverage needed", result.OverrideReason);
        Assert.NotNull(result.UpdatedBlock);
        Assert.Equal("#EF4444", result.UpdatedBlock.StatusColorCode); // Red / Misallocation override
        Assert.Equal(90.00m, result.UpdatedDailyLaborCost); // 6h * $15

        var persisted = await context.ShiftEntities.FindAsync(52);
        Assert.NotNull(persisted);
        Assert.Equal(102, persisted.EmployeeId);
    }

    [Fact]
    public async Task Handle_OvertimeRiskSingleShiftOver8HoursWithoutOverride_ReturnsFailure()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cashier" };
        var employee = new Employee
        {
            Id = 103,
            FirstName = "Charlie",
            LastName = "Brown",
            JobRoleId = 1,
            JobRole = role,
            IsActive = true,
            IsDeleted = false
        };

        var shiftDate = new DateTimeOffset(2026, 9, 9, 8, 0, 0, TimeSpan.Zero);
        var shift = new ShiftEntity
        {
            Id = 53,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = role,
            StartTime = shiftDate,
            EndTime = shiftDate.AddHours(8.5), // 8.5 hours > 8.0
            IsPublished = true,
            EmployeeId = null
        };

        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var command = new AssignEmployeeToShiftCommand(
            ShiftBlockId: 53,
            EmployeeId: 103,
            ConfirmOverride: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.HasConflicts);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(AssignmentConflictType.OvertimeRisk, warning.ConflictType);
        Assert.Equal("Assignment incurs overtime risk.", warning.Message);

        var persisted = await context.ShiftEntities.FindAsync(53);
        Assert.NotNull(persisted);
        Assert.Null(persisted.EmployeeId);
    }

    [Fact]
    public async Task Handle_OvertimeRiskWithOverride_CommitsAndReturnsRedStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cashier" };
        var employee = new Employee
        {
            Id = 104,
            FirstName = "David",
            LastName = "Miller",
            JobRoleId = 1,
            JobRole = role,
            IsActive = true,
            IsDeleted = false,
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 10m }
        };

        var shiftDate = new DateTimeOffset(2026, 9, 9, 8, 0, 0, TimeSpan.Zero);
        var shift = new ShiftEntity
        {
            Id = 54,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = role,
            StartTime = shiftDate,
            EndTime = shiftDate.AddHours(10), // 10 hours
            IsPublished = true,
            EmployeeId = null
        };

        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var command = new AssignEmployeeToShiftCommand(
            ShiftBlockId: 54,
            EmployeeId: 104,
            ConfirmOverride: true,
            OverrideReason: "Emergency extended shift"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.HasConflicts);
        Assert.True(result.WasOverridden);
        Assert.Equal("Emergency extended shift", result.OverrideReason);
        Assert.NotNull(result.UpdatedBlock);
        Assert.Equal("#EF4444", result.UpdatedBlock.StatusColorCode); // Red / Overtime override
        Assert.Equal(100.00m, result.UpdatedDailyLaborCost); // 10h * $10

        var persisted = await context.ShiftEntities.FindAsync(54);
        Assert.NotNull(persisted);
        Assert.Equal(104, persisted.EmployeeId);
    }

    [Fact]
    public async Task Handle_MultipleShiftsOnSameDay_TriggersOvertimeRisk()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cashier" };
        var employee = new Employee
        {
            Id = 105,
            FirstName = "Emma",
            LastName = "Watson",
            JobRoleId = 1,
            JobRole = role,
            IsActive = true,
            IsDeleted = false
        };

        var targetDate = new DateTimeOffset(2026, 9, 9, 8, 0, 0, TimeSpan.Zero);
        var existingShift = new ShiftEntity
        {
            Id = 60,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = role,
            EmployeeId = 105,
            StartTime = targetDate,
            EndTime = targetDate.AddHours(4),
            IsPublished = true
        };

        var secondShift = new ShiftEntity
        {
            Id = 61,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = role,
            EmployeeId = null,
            StartTime = targetDate.AddHours(5),
            EndTime = targetDate.AddHours(8),
            IsPublished = true
        };

        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.AddRange(existingShift, secondShift);
        await context.SaveChangesAsync();

        var command = new AssignEmployeeToShiftCommand(
            ShiftBlockId: 61,
            EmployeeId: 105,
            ConfirmOverride: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.HasConflicts);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(AssignmentConflictType.OvertimeRisk, warning.ConflictType);
        Assert.Equal("Assignment incurs overtime risk.", warning.Message);
    }

    [Fact]
    public async Task Handle_WeeklyHoursOverThreshold_TriggersOvertimeRisk()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cashier" };
        var employee = new Employee
        {
            Id = 106,
            FirstName = "Frank",
            LastName = "Castle",
            JobRoleId = 1,
            JobRole = role,
            IsActive = true,
            IsDeleted = false,
            PayrollProfile = new PayrollProfile
            {
                SalaryType = "Hourly",
                PaymentAmount = 20m,
                OvertimeThresholdHours = 35m // custom threshold 35h
            }
        };

        // 2026-09-07 is Monday
        var monday = new DateTimeOffset(2026, 9, 7, 9, 0, 0, TimeSpan.Zero);
        var tuesday = new DateTimeOffset(2026, 9, 8, 9, 0, 0, TimeSpan.Zero);
        var wednesday = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero);
        var thursday = new DateTimeOffset(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);
        var friday = new DateTimeOffset(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);

        // 4 shifts * 8h = 32 hours already worked this week
        var s1 = new ShiftEntity { Id = 71, SiteId = 10, JobRoleId = 1, EmployeeId = 106, StartTime = monday, EndTime = monday.AddHours(8), IsPublished = true };
        var s2 = new ShiftEntity { Id = 72, SiteId = 10, JobRoleId = 1, EmployeeId = 106, StartTime = tuesday, EndTime = tuesday.AddHours(8), IsPublished = true };
        var s3 = new ShiftEntity { Id = 73, SiteId = 10, JobRoleId = 1, EmployeeId = 106, StartTime = wednesday, EndTime = wednesday.AddHours(8), IsPublished = true };
        var s4 = new ShiftEntity { Id = 74, SiteId = 10, JobRoleId = 1, EmployeeId = 106, StartTime = thursday, EndTime = thursday.AddHours(8), IsPublished = true };

        // 5th shift on Friday: 8h -> total weekly hours = 40h > 35m threshold!
        var s5 = new ShiftEntity { Id = 75, SiteId = 10, JobRoleId = 1, JobRole = role, EmployeeId = null, StartTime = friday, EndTime = friday.AddHours(8), IsPublished = true };

        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.AddRange(s1, s2, s3, s4, s5);
        await context.SaveChangesAsync();

        var command = new AssignEmployeeToShiftCommand(
            ShiftBlockId: 75,
            EmployeeId: 106,
            ConfirmOverride: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.HasConflicts);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(AssignmentConflictType.OvertimeRisk, warning.ConflictType);
    }

    [Fact]
    public async Task Handle_NonExistentShiftBlockId_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var employee = new Employee { Id = 107, FirstName = "Grace", LastName = "Hopper", IsActive = true, IsDeleted = false };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var command = new AssignEmployeeToShiftCommand(
            ShiftBlockId: 9999,
            EmployeeId: 107
        );

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonExistentOrInactiveEmployeeId_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cashier" };
        var shift = new ShiftEntity
        {
            Id = 80,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = role,
            StartTime = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 9, 17, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };

        var inactiveEmployee = new Employee
        {
            Id = 108,
            FirstName = "Inactive",
            LastName = "User",
            IsActive = false,
            IsDeleted = false
        };

        var deletedEmployee = new Employee
        {
            Id = 109,
            FirstName = "Deleted",
            LastName = "User",
            IsActive = true,
            IsDeleted = true
        };

        context.JobRoles.Add(role);
        context.ShiftEntities.Add(shift);
        context.Employees.AddRange(inactiveEmployee, deletedEmployee);
        await context.SaveChangesAsync();

        // 1. Non-existent
        var cmdNonExistent = new AssignEmployeeToShiftCommand(80, 999);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(cmdNonExistent, CancellationToken.None));

        // 2. Inactive
        var cmdInactive = new AssignEmployeeToShiftCommand(80, 108);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(cmdInactive, CancellationToken.None));

        // 3. Deleted
        var cmdDeleted = new AssignEmployeeToShiftCommand(80, 109);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(cmdDeleted, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RecalculatesUpdatedDailyLaborCostCorrectly_AcrossSiteShifts()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Barista" };
        var emp1 = new Employee
        {
            Id = 201,
            FirstName = "Emp1",
            LastName = "Test",
            JobRoleId = 1,
            JobRole = role,
            IsActive = true,
            IsDeleted = false,
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 25m }
        };

        var emp2 = new Employee
        {
            Id = 202,
            FirstName = "Emp2",
            LastName = "Test",
            JobRoleId = 1,
            JobRole = role,
            IsActive = true,
            IsDeleted = false,
            PayrollProfile = new PayrollProfile { SalaryType = "Monthly", PaymentAmount = 3200m } // 3200 / 160 = $20/h
        };

        var targetDate = new DateTimeOffset(2026, 9, 9, 8, 0, 0, TimeSpan.Zero);

        // Pre-existing shift on same site & date for emp1: 4 hours * $25 = $100
        var existingShift = new ShiftEntity
        {
            Id = 91,
            SiteId = 5,
            JobRoleId = 1,
            EmployeeId = 201,
            StartTime = targetDate,
            EndTime = targetDate.AddHours(4),
            IsPublished = true
        };

        // Target shift to assign emp2 to: 5 hours * $20 = $100
        // Total expected daily labor cost after assignment = $200
        var targetShift = new ShiftEntity
        {
            Id = 92,
            SiteId = 5,
            JobRoleId = 1,
            JobRole = role,
            EmployeeId = null,
            StartTime = targetDate.AddHours(4),
            EndTime = targetDate.AddHours(9),
            IsPublished = true
        };

        context.JobRoles.Add(role);
        context.Employees.AddRange(emp1, emp2);
        context.ShiftEntities.AddRange(existingShift, targetShift);
        await context.SaveChangesAsync();

        var command = new AssignEmployeeToShiftCommand(
            ShiftBlockId: 92,
            EmployeeId: 202,
            ConfirmOverride: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(200.00m, result.UpdatedDailyLaborCost);
    }

    [Fact]
    public async Task Handle_UnpublishedShiftCompliant_ReturnsBlueColorCode()
    {
        // Arrange
        using var context = CreateDbContext();
        var (handler, _) = CreateHandler(context);

        var role = new JobRole { Id = 1, Title = "Cashier" };
        var employee = new Employee
        {
            Id = 301,
            FirstName = "Jane",
            LastName = "Unpublished",
            JobRoleId = 1,
            JobRole = role,
            IsActive = true,
            IsDeleted = false
        };

        var shiftDate = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero);
        var shift = new ShiftEntity
        {
            Id = 95,
            SiteId = 10,
            JobRoleId = 1,
            JobRole = role,
            StartTime = shiftDate,
            EndTime = shiftDate.AddHours(6),
            IsPublished = false, // Draft / Unpublished
            EmployeeId = null
        };

        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var command = new AssignEmployeeToShiftCommand(
            ShiftBlockId: 95,
            EmployeeId: 301
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.UpdatedBlock);
        Assert.Equal("#3B82F6", result.UpdatedBlock.StatusColorCode); // Blue / Unpublished
    }
}
