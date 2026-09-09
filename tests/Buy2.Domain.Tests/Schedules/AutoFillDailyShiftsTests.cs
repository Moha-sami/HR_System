using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.AutoFillDailyShifts;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Schedules;

public class AutoFillDailyShiftsTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static AutoFillDailyShiftsCommandHandler CreateHandler(Buy2DbContext context)
    {
        return new AutoFillDailyShiftsCommandHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context),
            new GenericRepository<SitePreferredEmployee>(context),
            new UnitOfWork(context),
            new GenericRepository<SiteOperationalHour>(context)
        );
    }

    [Fact]
    public async Task Handle_SiteNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = CreateHandler(context);
        var command = new AutoFillDailyShiftsCommand(SiteId: 999, Date: new DateOnly(2026, 9, 9));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Site with ID 999 not found", ex.Message);
    }

    [Fact]
    public async Task Handle_NoOpenBlocks_ReturnsZeroAssignedAndZeroUnfillable()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9);
        var site = new Site { Id = 1, SiteName = "Downtown Hub" };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new AutoFillDailyShiftsCommand(SiteId: 1, Date: targetDate);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SiteId);
        Assert.Equal(targetDate, result.Date);
        Assert.Equal(0, result.TotalOpenBlocksScanned);
        Assert.Equal(0, result.SuccessfullyAssignedCount);
        Assert.Equal(0, result.UnfillableCount);
        Assert.Empty(result.AssignedBlocks);
        Assert.Equal(WeekDayCalendarStatus.NoAllocations, result.CoverageStatus);
    }

    [Fact]
    public async Task Handle_AssignsMatchingQualifiedEmployee_PreservesManualAssignments()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9);
        var site = new Site { Id = 1, SiteName = "North Branch" };
        var role = new JobRole { Id = 10, Title = "Barista" };

        var existingEmp = new Employee
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Smith",
            JobRoleId = 10,
            IsActive = true,
            IsDeleted = false,
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 20m }
        };

        var candidateEmp = new Employee
        {
            Id = 2,
            FirstName = "Bob",
            LastName = "Jones",
            JobRoleId = 10,
            IsActive = true,
            IsDeleted = false,
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 22m }
        };

        var shiftAssigned = new ShiftEntity
        {
            Id = 101,
            SiteId = 1,
            JobRoleId = 10,
            EmployeeId = 1,
            StartTime = new DateTimeOffset(2026, 9, 9, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };

        var shiftOpen = new ShiftEntity
        {
            Id = 102,
            SiteId = 1,
            JobRoleId = 10,
            EmployeeId = null,
            StartTime = new DateTimeOffset(2026, 9, 9, 13, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 9, 17, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.AddRange(existingEmp, candidateEmp);
        context.ShiftEntities.AddRange(shiftAssigned, shiftOpen);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new AutoFillDailyShiftsCommand(SiteId: 1, Date: targetDate);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalOpenBlocksScanned);
        Assert.Equal(1, result.SuccessfullyAssignedCount);
        Assert.Equal(0, result.UnfillableCount);
        Assert.Single(result.AssignedBlocks);

        var assignment = result.AssignedBlocks[0];
        Assert.Equal(102, assignment.ShiftId);
        Assert.Equal(2, assignment.EmployeeId);
        Assert.Equal("Bob Jones", assignment.EmployeeName);

        // Verify existing assignment untouched in DB
        var dbAssigned = await context.ShiftEntities.FindAsync(101);
        Assert.NotNull(dbAssigned);
        Assert.Equal(1, dbAssigned.EmployeeId);

        // Verify open block now has EmployeeId 2 in DB
        var dbOpen = await context.ShiftEntities.FindAsync(102);
        Assert.NotNull(dbOpen);
        Assert.Equal(2, dbOpen.EmployeeId);
    }

    [Fact]
    public async Task Handle_PrioritizesPreferredEmployeeOverNonPreferred()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9);
        var site = new Site { Id = 1, SiteName = "West Store" };
        var role = new JobRole { Id = 5, Title = "Cashier" };

        var regularEmp = new Employee
        {
            Id = 10,
            FirstName = "Regular",
            LastName = "Employee",
            JobRoleId = 5,
            IsActive = true,
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 15m }
        };

        var preferredEmp = new Employee
        {
            Id = 20,
            FirstName = "Preferred",
            LastName = "Employee",
            JobRoleId = 5,
            IsActive = true,
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 15m }
        };

        var openShift = new ShiftEntity
        {
            Id = 201,
            SiteId = 1,
            JobRoleId = 5,
            EmployeeId = null,
            StartTime = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 9, 17, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };

        var pref = new SitePreferredEmployee { SiteId = 1, EmployeeId = 20 };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.AddRange(regularEmp, preferredEmp);
        context.ShiftEntities.Add(openShift);
        context.SitePreferredEmployees.Add(pref);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new AutoFillDailyShiftsCommand(SiteId: 1, Date: targetDate);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessfullyAssignedCount);
        var assigned = Assert.Single(result.AssignedBlocks);
        Assert.Equal(20, assigned.EmployeeId);
        Assert.True(assigned.IsPreferredEmployee);
    }

    [Fact]
    public async Task Handle_PrioritizesLowestWeeklyHours_WhenPreferredStatusMatches()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9); // Wednesday
        var site = new Site { Id = 1, SiteName = "Central Hub" };
        var role = new JobRole { Id = 8, Title = "Stock Clerk" };

        var busyEmp = new Employee
        {
            Id = 1,
            FirstName = "Busy",
            LastName = "Worker",
            JobRoleId = 8,
            IsActive = true,
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 18m, OvertimeThresholdHours = 40m }
        };

        var freeEmp = new Employee
        {
            Id = 2,
            FirstName = "Free",
            LastName = "Worker",
            JobRoleId = 8,
            IsActive = true,
            PayrollProfile = new PayrollProfile { SalaryType = "Hourly", PaymentAmount = 18m, OvertimeThresholdHours = 40m }
        };

        // busyEmp has a 20-hour shift earlier in the week (Monday)
        var priorShift = new ShiftEntity
        {
            Id = 300,
            SiteId = 1,
            JobRoleId = 8,
            EmployeeId = 1,
            StartTime = new DateTimeOffset(2026, 9, 7, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 7, 16, 0, 0, TimeSpan.Zero), // 8 hours
            IsPublished = true
        };

        var openShift = new ShiftEntity
        {
            Id = 301,
            SiteId = 1,
            JobRoleId = 8,
            EmployeeId = null,
            StartTime = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 9, 17, 0, 0, TimeSpan.Zero), // 8 hours
            IsPublished = true
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.AddRange(busyEmp, freeEmp);
        context.ShiftEntities.AddRange(priorShift, openShift);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new AutoFillDailyShiftsCommand(SiteId: 1, Date: targetDate);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessfullyAssignedCount);
        var assigned = Assert.Single(result.AssignedBlocks);
        Assert.Equal(2, assigned.EmployeeId); // freeEmp chosen due to lower scheduled hours
    }

    [Fact]
    public async Task Handle_DeterministicTieBreaker_PicksHighestEmployeeId()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9);
        var site = new Site { Id = 1, SiteName = "East Side" };
        var role = new JobRole { Id = 3, Title = "Helper" };

        var empLowId = new Employee { Id = 15, FirstName = "Low", LastName = "Id", JobRoleId = 3, IsActive = true };
        var empHighId = new Employee { Id = 75, FirstName = "High", LastName = "Id", JobRoleId = 3, IsActive = true };

        var openShift = new ShiftEntity
        {
            Id = 401,
            SiteId = 1,
            JobRoleId = 3,
            EmployeeId = null,
            StartTime = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 9, 13, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.AddRange(empLowId, empHighId);
        context.ShiftEntities.Add(openShift);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new AutoFillDailyShiftsCommand(SiteId: 1, Date: targetDate);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var assigned = Assert.Single(result.AssignedBlocks);
        Assert.Equal(75, assigned.EmployeeId);
    }

    [Fact]
    public async Task Handle_AvoidsAssigningEmployeeToOverlappingShifts()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9);
        var site = new Site { Id = 1, SiteName = "Overlap Site" };
        var role = new JobRole { Id = 4, Title = "Baker" };

        var emp = new Employee { Id = 10, FirstName = "Cook", LastName = "Chef", JobRoleId = 4, IsActive = true };

        // emp already has a shift on target date from 08:00 to 12:00 at another site (or same site)
        var existingShift = new ShiftEntity
        {
            Id = 501,
            SiteId = 2,
            JobRoleId = 4,
            EmployeeId = 10,
            StartTime = new DateTimeOffset(2026, 9, 9, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };

        // Open shift from 10:00 to 14:00 (overlaps with 08:00 - 12:00)
        var overlappingBlock = new ShiftEntity
        {
            Id = 502,
            SiteId = 1,
            JobRoleId = 4,
            EmployeeId = null,
            StartTime = new DateTimeOffset(2026, 9, 9, 10, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 9, 14, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.Add(emp);
        context.ShiftEntities.AddRange(existingShift, overlappingBlock);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new AutoFillDailyShiftsCommand(SiteId: 1, Date: targetDate);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalOpenBlocksScanned);
        Assert.Equal(0, result.SuccessfullyAssignedCount);
        Assert.Equal(1, result.UnfillableCount);
        Assert.Empty(result.AssignedBlocks);

        var dbBlock = await context.ShiftEntities.FindAsync(502);
        Assert.Null(dbBlock!.EmployeeId);
    }

    [Fact]
    public async Task Handle_AvoidsAssigningIfExceedsDaily8HourLimit()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9);
        var site = new Site { Id = 1, SiteName = "Overtime Site" };
        var role = new JobRole { Id = 2, Title = "Clerk" };

        var emp = new Employee { Id = 50, FirstName = "Worker", LastName = "One", JobRoleId = 2, IsActive = true };

        // Open shift of 9 hours (exceeds 8-hour daily limit)
        var longShift = new ShiftEntity
        {
            Id = 601,
            SiteId = 1,
            JobRoleId = 2,
            EmployeeId = null,
            StartTime = new DateTimeOffset(2026, 9, 9, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 9, 17, 0, 0, TimeSpan.Zero).AddHours(1), // 9 hours
            IsPublished = true
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.Add(emp);
        context.ShiftEntities.Add(longShift);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new AutoFillDailyShiftsCommand(SiteId: 1, Date: targetDate);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessfullyAssignedCount);
        Assert.Equal(1, result.UnfillableCount);
    }

    [Fact]
    public async Task Handle_AvoidsAssigningIfExceedsWeeklyOvertimeThreshold()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9); // Wednesday
        var site = new Site { Id = 1, SiteName = "Weekly Overtime Site" };
        var role = new JobRole { Id = 6, Title = "Technician" };

        var emp = new Employee
        {
            Id = 60,
            FirstName = "Tech",
            LastName = "Guy",
            JobRoleId = 6,
            IsActive = true,
            PayrollProfile = new PayrollProfile
            {
                SalaryType = "Hourly",
                PaymentAmount = 30m,
                OvertimeThresholdHours = 35m
            }
        };

        // Already scheduled for 32 hours this week (Mon-Tue)
        var monShift = new ShiftEntity
        {
            Id = 701,
            SiteId = 1,
            JobRoleId = 6,
            EmployeeId = 60,
            StartTime = new DateTimeOffset(2026, 9, 7, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 7, 8, 0, 0, TimeSpan.Zero).AddHours(16), // 16 hours
            IsPublished = true
        };
        var tueShift = new ShiftEntity
        {
            Id = 702,
            SiteId = 1,
            JobRoleId = 6,
            EmployeeId = 60,
            StartTime = new DateTimeOffset(2026, 9, 8, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 8, 8, 0, 0, TimeSpan.Zero).AddHours(16), // 16 hours
            IsPublished = true
        };

        // Open shift for 6 hours. 32 + 6 = 38 > 35m threshold
        var openShift = new ShiftEntity
        {
            Id = 703,
            SiteId = 1,
            JobRoleId = 6,
            EmployeeId = null,
            StartTime = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 9, 15, 0, 0, TimeSpan.Zero), // 6 hours
            IsPublished = true
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.Add(emp);
        context.ShiftEntities.AddRange(monShift, tueShift, openShift);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new AutoFillDailyShiftsCommand(SiteId: 1, Date: targetDate);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.SuccessfullyAssignedCount);
        Assert.Equal(1, result.UnfillableCount);
    }

    [Fact]
    public async Task Handle_NoMatchingRole_LeavesBlockUnassigned_IncrementsUnfillableCount()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9);
        var site = new Site { Id = 1, SiteName = "Role Test Site" };
        var role1 = new JobRole { Id = 1, Title = "Chef" };
        var role2 = new JobRole { Id = 2, Title = "Driver" };

        var emp = new Employee { Id = 1, FirstName = "Only", LastName = "Chef", JobRoleId = 1, IsActive = true };

        var openShiftForDriver = new ShiftEntity
        {
            Id = 801,
            SiteId = 1,
            JobRoleId = 2, // Driver needed
            EmployeeId = null,
            StartTime = new DateTimeOffset(2026, 9, 9, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 9, 14, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };

        context.Sites.Add(site);
        context.JobRoles.AddRange(role1, role2);
        context.Employees.Add(emp);
        context.ShiftEntities.Add(openShiftForDriver);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new AutoFillDailyShiftsCommand(SiteId: 1, Date: targetDate);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalOpenBlocksScanned);
        Assert.Equal(0, result.SuccessfullyAssignedCount);
        Assert.Equal(1, result.UnfillableCount);
        Assert.Empty(result.AssignedBlocks);
    }

    [Fact]
    public async Task Handle_CalculatesUpdatedDailyLaborCostAndSiteCoverageStatusCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9);
        var site = new Site
        {
            Id = 1,
            SiteName = "Cost Site",
            OperationalHours = new List<SiteOperationalHour>
            {
                new() { DayOfWeek = DayOfWeek.Wednesday, IsOpen = true }
            }
        };

        var role = new JobRole { Id = 1, Title = "Cashier" };

        var emp = new Employee
        {
            Id = 1,
            FirstName = "Sara",
            LastName = "Connor",
            JobRoleId = 1,
            IsActive = true,
            PayrollProfile = new PayrollProfile
            {
                SalaryType = "Hourly",
                PaymentAmount = 25m
            }
        };

        // Open shift for 4 hours -> 4 * 25 = 100m
        var openShift = new ShiftEntity
        {
            Id = 901,
            SiteId = 1,
            JobRoleId = 1,
            EmployeeId = null,
            StartTime = new DateTimeOffset(2026, 9, 9, 10, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 9, 14, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.Add(emp);
        context.ShiftEntities.Add(openShift);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new AutoFillDailyShiftsCommand(SiteId: 1, Date: targetDate);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SuccessfullyAssignedCount);
        Assert.Equal(100m, result.UpdatedDailyLaborCost);
        Assert.Equal(WeekDayCalendarStatus.CoveredAndPublished, result.CoverageStatus);
    }

    [Fact]
    public async Task Handle_DayOffSite_ReturnsDimmedDayOffStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var targetDate = new DateOnly(2026, 9, 9); // Wednesday
        var site = new Site
        {
            Id = 1,
            SiteName = "Closed Site",
            OperationalHours = new List<SiteOperationalHour>
            {
                new() { DayOfWeek = DayOfWeek.Wednesday, IsOpen = false }
            }
        };

        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new AutoFillDailyShiftsCommand(SiteId: 1, Date: targetDate);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(WeekDayCalendarStatus.DimmedDayOff, result.CoverageStatus);
    }

    [Fact]
    public async Task ShiftsOverviewController_AutoFillDailyShifts_DispatchesCommand()
    {
        // Arrange
        var targetDate = new DateOnly(2026, 9, 9);
        var expectedResponse = new AutoFillDailyShiftsResponseDto(
            SiteId: 1,
            Date: targetDate,
            TotalOpenBlocksScanned: 2,
            SuccessfullyAssignedCount: 2,
            UnfillableCount: 0,
            AssignedBlocks: new List<AutoFillAssignmentDetailDto>(),
            UpdatedDailyLaborCost: 200m,
            CoverageStatus: WeekDayCalendarStatus.CoveredAndPublished,
            SummaryMessage: "Done"
        );

        var fakeMediator = new FakeMediator((req, ct) =>
        {
            if (req is AutoFillDailyShiftsCommand cmd)
            {
                Assert.Equal(1, cmd.SiteId);
                Assert.Equal(targetDate, cmd.Date);
                return Task.FromResult<object?>(expectedResponse);
            }
            return Task.FromResult<object?>(null);
        });

        var controller = new ShiftsOverviewController(fakeMediator);
        var requestDto = new AutoFillDailyShiftsRequestDto(SiteId: 1, Date: targetDate);

        // Act
        var actionResult = await controller.AutoFillDailyShifts(requestDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<AutoFillDailyShiftsResponseDto>(okResult.Value);
        Assert.Equal(1, response.SiteId);
        Assert.Equal(2, response.SuccessfullyAssignedCount);
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
