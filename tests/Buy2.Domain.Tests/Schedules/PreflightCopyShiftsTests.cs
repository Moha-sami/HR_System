using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.PreflightCopyShifts;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Schedules;

public class PreflightCopyShiftsTests
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
        var handler = new PreflightCopyShiftsQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var query = new PreflightCopyShiftsQuery(
            SiteId: 999,
            SourceDate: new DateOnly(2026, 9, 9),
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 10) }
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NeitherTargetDatesNorRecurringDays_ThrowsArgumentException()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Branch 1" });
        await context.SaveChangesAsync();

        var handler = new PreflightCopyShiftsQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var queryNull = new PreflightCopyShiftsQuery(
            SiteId: 1,
            SourceDate: new DateOnly(2026, 9, 9),
            TargetDates: null,
            RecurringDays: null
        );

        var queryEmpty = new PreflightCopyShiftsQuery(
            SiteId: 1,
            SourceDate: new DateOnly(2026, 9, 9),
            TargetDates: new List<DateOnly>(),
            RecurringDays: new List<DayOfWeek>()
        );

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(queryNull, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(queryEmpty, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExplicitDates_DetectsConflictsAndCleanDates()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Branch 1" };
        var employee = new Employee { Id = 10, FirstName = "John", LastName = "Doe" };
        var role = new JobRole { Id = 5, Title = "Cashier" };

        var sourceDate = new DateOnly(2026, 9, 1);
        var conflictingDate = new DateOnly(2026, 9, 2);
        var cleanDate = new DateOnly(2026, 9, 3);

        var shiftTime = new DateTimeOffset(conflictingDate.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
        var shift = new ShiftEntity
        {
            Id = 101,
            SiteId = 1,
            EmployeeId = 10,
            JobRoleId = 5,
            JobRole = role,
            StartTime = shiftTime,
            EndTime = shiftTime.AddHours(8)
        };

        context.Sites.Add(site);
        context.Employees.Add(employee);
        context.JobRoles.Add(role);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = new PreflightCopyShiftsQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var query = new PreflightCopyShiftsQuery(
            SiteId: 1,
            SourceDate: sourceDate,
            TargetDates: new List<DateOnly> { sourceDate, conflictingDate, cleanDate }
        );

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, result.SiteId);
        Assert.Equal(sourceDate, result.SourceDate);
        Assert.Equal(2, result.TotalTargetDates);
        Assert.True(result.HasConflicts);

        Assert.Single(result.ConflictFreeDates);
        Assert.Contains(cleanDate, result.ConflictFreeDates);

        Assert.Single(result.ConflictingDates);
        var conflict = result.ConflictingDates[0];
        Assert.Equal(conflictingDate, conflict.Date);
        Assert.Equal(1, conflict.ShiftCount);
        Assert.Contains("Cashier", conflict.ShiftNames);

        var existing = conflict.ExistingShifts[0];
        Assert.Equal(101, existing.ShiftId);
        Assert.Equal("Cashier", existing.ShiftName);
        Assert.Equal("John Doe", existing.EmployeeName);
        Assert.Equal(new TimeSpan(8, 0, 0), existing.StartTime);
        Assert.Equal(new TimeSpan(16, 0, 0), existing.EndTime);
    }

    [Fact]
    public async Task Handle_RecurringWeekdays_ExpandsAndEvaluatesConflicts()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Branch 1" };
        var role = new JobRole { Id = 7, Title = "Barista" };

        var sourceDate = new DateOnly(2026, 9, 2); // Wednesday
        var conflictDate = new DateOnly(2026, 9, 9);
        var shiftTime = new DateTimeOffset(conflictDate.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);
        var shift = new ShiftEntity
        {
            Id = 202,
            SiteId = 1,
            JobRoleId = 7,
            JobRole = role,
            StartTime = shiftTime,
            EndTime = shiftTime.AddHours(6)
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var handler = new PreflightCopyShiftsQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var query = new PreflightCopyShiftsQuery(
            SiteId: 1,
            SourceDate: sourceDate,
            RecurringDays: new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday },
            WeekCount: 2
        );

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(4, result.TotalTargetDates);
        Assert.True(result.HasConflicts);

        Assert.Equal(3, result.ConflictFreeDates.Count);
        Assert.Contains(new DateOnly(2026, 9, 7), result.ConflictFreeDates);
        Assert.Contains(new DateOnly(2026, 9, 14), result.ConflictFreeDates);
        Assert.Contains(new DateOnly(2026, 9, 16), result.ConflictFreeDates);

        Assert.Single(result.ConflictingDates);
        Assert.Equal(conflictDate, result.ConflictingDates[0].Date);
        Assert.Equal(1, result.ConflictingDates[0].ShiftCount);
        Assert.Null(result.ConflictingDates[0].ExistingShifts[0].EmployeeName);
    }

    [Fact]
    public async Task Handle_MixedTargets_CombinesExplicitAndRecurringDistinctly()
    {
        using var context = CreateDbContext();
        context.Sites.Add(new Site { Id = 1, SiteName = "Branch 1" });
        await context.SaveChangesAsync();

        var handler = new PreflightCopyShiftsQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var sourceDate = new DateOnly(2026, 9, 2); // Wednesday
        var query = new PreflightCopyShiftsQuery(
            SiteId: 1,
            SourceDate: sourceDate,
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 10) },
            RecurringDays: new List<DayOfWeek> { DayOfWeek.Monday },
            WeekCount: 1
        );

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.TotalTargetDates);
        Assert.Equal(2, result.ConflictFreeDates.Count);
        Assert.Empty(result.ConflictingDates);
        Assert.False(result.HasConflicts);
        Assert.Equal(new DateOnly(2026, 9, 7), result.ConflictFreeDates[0]);
        Assert.Equal(new DateOnly(2026, 9, 10), result.ConflictFreeDates[1]);
    }

    [Fact]
    public async Task Handle_ShiftNamesDisplay_FollowsPrecedenceRules()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Branch 1" };
        var role = new JobRole { Id = 2, Title = "Supervisor" };
        var template = new ShiftTemplate { Id = 10, Name = "Opening Shift" };

        var targetDate = new DateOnly(2026, 9, 15);
        var baseTime = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);

        var shiftWithTemplate = new ShiftEntity
        {
            Id = 301,
            SiteId = 1,
            JobRoleId = 2,
            JobRole = role,
            ShiftTemplateId = 10,
            ShiftTemplate = template,
            StartTime = baseTime,
            EndTime = baseTime.AddHours(4)
        };

        var shiftWithRoleOnly = new ShiftEntity
        {
            Id = 302,
            SiteId = 1,
            JobRoleId = 2,
            JobRole = role,
            StartTime = baseTime.AddHours(4),
            EndTime = baseTime.AddHours(8)
        };

        var roleWithEmptyTitle = new JobRole { Id = 3, Title = "" };
        var shiftFallback = new ShiftEntity
        {
            Id = 303,
            SiteId = 1,
            JobRoleId = 3,
            JobRole = roleWithEmptyTitle,
            StartTime = baseTime.AddHours(8),
            EndTime = baseTime.AddHours(12)
        };

        context.Sites.Add(site);
        context.JobRoles.AddRange(role, roleWithEmptyTitle);
        context.ShiftTemplates.Add(template);
        context.ShiftEntities.AddRange(shiftWithTemplate, shiftWithRoleOnly, shiftFallback);
        await context.SaveChangesAsync();

        var handler = new PreflightCopyShiftsQueryHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context)
        );

        var query = new PreflightCopyShiftsQuery(
            SiteId: 1,
            SourceDate: new DateOnly(2026, 9, 1),
            TargetDates: new List<DateOnly> { targetDate }
        );

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Single(result.ConflictingDates);
        var conflict = result.ConflictingDates[0];
        Assert.Equal(3, conflict.ShiftCount);

        Assert.Equal("Opening Shift", conflict.ExistingShifts[0].ShiftName);
        Assert.Equal("Supervisor", conflict.ExistingShifts[1].ShiftName);
        Assert.Equal("Shift #303", conflict.ExistingShifts[2].ShiftName);

        Assert.Equal(new List<string> { "Opening Shift", "Supervisor", "Shift #303" }, conflict.ShiftNames);
    }

    [Fact]
    public async Task Controller_PreflightCopyShifts_ReturnsOkWithResponse()
    {
        var expectedResponse = new PreflightCopyShiftsResponseDto(
            SiteId: 1,
            SourceDate: new DateOnly(2026, 9, 1),
            TotalTargetDates: 1,
            ConflictFreeDates: new List<DateOnly> { new DateOnly(2026, 9, 2) },
            ConflictingDates: new List<ConflictingDateDto>(),
            HasConflicts: false
        );

        var fakeMediator = new FakeMediator((req, ct) => Task.FromResult<object?>(expectedResponse));
        var controller = new ScheduleValidationController(fakeMediator);

        var requestDto = new PreflightCopyShiftsRequestDto(
            SiteId: 1,
            SourceDate: new DateOnly(2026, 9, 1),
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 2) }
        );

        var actionResult = await controller.PreflightCopyShifts(requestDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var responseValue = Assert.IsType<PreflightCopyShiftsResponseDto>(okResult.Value);
        Assert.Equal(1, responseValue.SiteId);
        Assert.False(responseValue.HasConflicts);
    }

    [Fact]
    public async Task Controller_PreflightCopyShifts_ReturnsNotFound_WhenSiteMissing()
    {
        var fakeMediator = new FakeMediator((req, ct) => throw new KeyNotFoundException("Site not found"));
        var controller = new ScheduleValidationController(fakeMediator);

        var requestDto = new PreflightCopyShiftsRequestDto(
            SiteId: 999,
            SourceDate: new DateOnly(2026, 9, 1),
            TargetDates: new List<DateOnly> { new DateOnly(2026, 9, 2) }
        );

        var actionResult = await controller.PreflightCopyShifts(requestDto, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task Controller_PreflightCopyShifts_ReturnsBadRequest_WhenArgumentInvalid()
    {
        var fakeMediator = new FakeMediator((req, ct) => throw new ArgumentException("Invalid arguments"));
        var controller = new ScheduleValidationController(fakeMediator);

        var requestDto = new PreflightCopyShiftsRequestDto(
            SiteId: 1,
            SourceDate: new DateOnly(2026, 9, 1)
        );

        var actionResult = await controller.PreflightCopyShifts(requestDto, CancellationToken.None);

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
