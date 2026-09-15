using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Buy2.Api.Services.PointsAutomation;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.DTOs;
using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Authentication.ResetPassword;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Application.Features.Jobs.GetJobs;
using Buy2.Application.Features.Points.Automation;
using Buy2.Application.Features.Schedules.GetSiteShiftsOverview;
using Buy2.Application.Features.ShiftMarket.GetOpenShifts;
using Buy2.Application.Features.ShiftTemplates.DuplicateShiftTemplate;
using Buy2.Application.Features.Sites.GetSites;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Buy2.Domain.Tests.Performance;

public class PerformanceOptimizationReproductionTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    public class TrackingRepository<T> : IRepository<T> where T : class
    {
        private readonly GenericRepository<T> _inner;
        public int GetAllAsyncCallCount { get; private set; }
        public int ReadCallCount { get; private set; }
        public int AnyAsyncCallCount { get; private set; }
        public List<string> CapturedSpecifications { get; } = new();

        public TrackingRepository(Buy2DbContext context)
        {
            _inner = new GenericRepository<T>(context);
        }

        private void Capture(ISpecification<T> specification, [CallerMemberName] string? caller = null)
        {
            ReadCallCount++;
            CapturedSpecifications.Add(
                $"{caller} | Criteria:{specification.Criteria} | Includes:[{string.Join(",", specification.Includes)}] | Orderings:{specification.Orderings.Count} | IgnoreFilters:{specification.IgnoreQueryFilters} | Tracked:{specification.Tracked}");
        }

        private void CapturePredicate(Expression<Func<T, bool>>? predicate, string[] includes, [CallerMemberName] string? caller = null)
        {
            ReadCallCount++;
            CapturedSpecifications.Add($"{caller} | Criteria:{predicate} | Includes:[{string.Join(",", includes)}]");
        }

        public Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            Capture(specification);
            return _inner.FirstOrDefaultAsync(specification, cancellationToken);
        }

        public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params string[] includes)
        {
            CapturePredicate(predicate, includes);
            return _inner.FirstOrDefaultAsync(predicate, cancellationToken, includes);
        }

        public Task<TResult?> FirstOrDefaultAsync<TResult>(ISpecification<T> specification, Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
        {
            Capture(specification);
            return _inner.FirstOrDefaultAsync(specification, selector, cancellationToken);
        }

        public Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            Capture(specification);
            return _inner.ListAsync(specification, cancellationToken);
        }

        public Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default, params string[] includes)
        {
            CapturePredicate(predicate, includes);
            return _inner.ListAsync(predicate, cancellationToken, includes);
        }

        public Task<List<TResult>> ListAsync<TResult>(ISpecification<T> specification, Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
        {
            Capture(specification);
            return _inner.ListAsync(specification, selector, cancellationToken);
        }

        public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            ReadCallCount++;
            return _inner.CountAsync(predicate, cancellationToken);
        }

        public Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            Capture(specification);
            return _inner.CountAsync(specification, cancellationToken);
        }

        public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            AnyAsyncCallCount++;
            return _inner.AnyAsync(predicate, cancellationToken);
        }

        public Task<int> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, int>> selector, CancellationToken cancellationToken = default)
        {
            ReadCallCount++;
            return _inner.SumAsync(predicate, selector, cancellationToken);
        }

        public Task<int?> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, int?>> selector, CancellationToken cancellationToken = default)
        {
            ReadCallCount++;
            return _inner.SumAsync(predicate, selector, cancellationToken);
        }

        public Task<decimal> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
        {
            ReadCallCount++;
            return _inner.SumAsync(predicate, selector, cancellationToken);
        }

        public Task<decimal?> SumAsync(Expression<Func<T, bool>>? predicate, Expression<Func<T, decimal?>> selector, CancellationToken cancellationToken = default)
        {
            ReadCallCount++;
            return _inner.SumAsync(predicate, selector, cancellationToken);
        }

        public Task<PagedResult<T>> PagedAsync(ISpecification<T> specification, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            Capture(specification);
            return _inner.PagedAsync(specification, pageNumber, pageSize, cancellationToken);
        }

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            GetAllAsyncCallCount++;
            return await _inner.GetAllAsync(cancellationToken);
        }

        public Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            _inner.GetByIdAsync(id, cancellationToken);

        public Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
            _inner.AddAsync(entity, cancellationToken);

        public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) =>
            _inner.AddRangeAsync(entities, cancellationToken);

        public void Update(T entity) => _inner.Update(entity);
        public void Delete(T entity) => _inner.Delete(entity);
    }

    private class FakePointsAutomationRunner : IPointsAutomationRunner
    {
        public Task<AutomationJobResultDto?> RunAsync(
            AutomationCategory category,
            AutomationPeriod period,
            DateTimeOffset periodStartUtc,
            DateTimeOffset periodEndUtc,
            CancellationToken cancellationToken = default)
        {
            var result = new AutomationJobResultDto(
                Guid.NewGuid().ToString(),
                DateTimeOffset.UtcNow,
                5,
                100,
                0,
                5,
                new List<int>(),
                new List<EmployeeAutomationFailureDto>(),
                new List<EmployeeAutomationSuccessDto>(),
                "Evaluation complete"
            );
            return Task.FromResult<AutomationJobResultDto?>(result);
        }
    }

    // Issue #310: ResetPasswordCommand in-memory filtering via GetAllAsync()
    [Fact]
    public async Task Issue310_ResetPasswordCommand_ShouldNotUseGetAllAsync_ShouldQueryDirectly()
    {
        using var context = CreateDbContext();
        var emp = new Employee
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@buy2.com",
            EmployeeCode = "EMP001",
            JobRoleId = 1
        };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var repo = new TrackingRepository<Employee>(context);
        var handler = new ResetPasswordCommandHandler(repo);

        var result = await handler.Handle(new ResetPasswordCommand("john@buy2.com", "NewPass123!"), CancellationToken.None);

        Assert.True(result);
        Assert.True(repo.GetAllAsyncCallCount == 0,
            "Issue #310: ResetPasswordCommandHandler calls GetAllAsync(), loading the entire Employees table into memory before filtering.");
    }

    // Issue #311: GetOpenShiftsQuery in-memory filtering via GetAllAsync()
    [Fact]
    public async Task Issue311_GetOpenShiftsQuery_ShouldNotUseGetAllAsync_ShouldFilterAtDatabaseLevel()
    {
        using var context = CreateDbContext();
        var shift = new ShiftEntity
        {
            Id = 1,
            SiteId = 10,
            JobRoleId = 1,
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(8),
            IsPublished = true,
            EmployeeId = null
        };
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var repo = new TrackingRepository<ShiftEntity>(context);
        var handler = new GetOpenShiftsQueryHandler(repo);

        var result = await handler.Handle(new GetOpenShiftsQuery(), CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.True(repo.GetAllAsyncCallCount == 0,
            "Issue #311: GetOpenShiftsQueryHandler calls GetAllAsync(), loading all shifts into memory before filtering published open shifts.");
    }

    // Issue #312: GetSitesQuery unbounded GetAllAsync() without projection
    [Fact]
    public async Task Issue312_GetSitesQuery_ShouldNotUseGetAllAsync_ShouldUseQueryWithProjection()
    {
        using var context = CreateDbContext();
        var site = new Site
        {
            Id = 1,
            SiteName = "HQ Site",
            Latitude = 30.0,
            Longitude = 31.0
        };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var repo = new TrackingRepository<Site>(context);
        var handler = new GetSitesQueryHandler(repo);

        var result = await handler.Handle(new GetSitesQuery(), CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.True(repo.GetAllAsyncCallCount == 0,
            "Issue #312: GetSitesQueryHandler calls GetAllAsync(), loading all sites into memory without query projection.");
    }

    // Issue #313: PointsAutomationDispatcher N+1 queries in catch-up loop
    [Fact]
    public async Task Issue313_PointsAutomationDispatcher_ShouldNotExecuteNPlusOneQueriesInCatchUpLoop()
    {
        using var context = CreateDbContext();
        var setting = new PointsAutomationSetting
        {
            Id = 1,
            Category = AutomationCategory.TimeAndAttendance,
            AutomationPeriod = AutomationPeriod.Daily,
            IsEnabled = true
        };
        context.PointsAutomationSettings.Add(setting);

        // Seed a completed run from 5 days ago to generate 3 catch-up windows
        var lastCompleted = new PointsAutomationRun
        {
            Id = 1,
            Category = AutomationCategory.TimeAndAttendance,
            AutomationPeriod = AutomationPeriod.Daily,
            Status = AutomationRunStatus.Completed,
            PeriodStart = DateTimeOffset.UtcNow.AddDays(-5),
            PeriodEnd = DateTimeOffset.UtcNow.AddDays(-4),
            ExecutedAt = DateTimeOffset.UtcNow.AddDays(-4)
        };
        context.PointsAutomationRuns.Add(lastCompleted);
        await context.SaveChangesAsync();

        var settingsRepo = new TrackingRepository<PointsAutomationSetting>(context);
        var runsRepo = new TrackingRepository<PointsAutomationRun>(context);
        var runner = new FakePointsAutomationRunner();
        var options = Options.Create(new PointsAutomationOptions
        {
            Enabled = true,
            MaxCatchUpWindowsPerPeriod = 3,
            TimeZone = "Egypt Standard Time"
        });
        var logger = NullLogger<PointsAutomationDispatcher>.Instance;

        var dispatcher = new PointsAutomationDispatcher(runner, settingsRepo, runsRepo, options, logger);
        await dispatcher.DispatchDailyAsync(CancellationToken.None);

        Assert.True(runsRepo.ReadCallCount <= 1,
            $"Issue #313: PointsAutomationDispatcher executed {runsRepo.ReadCallCount} read queries across catch-up windows (N+1 query anti-pattern). Expected at most 1 query.");
    }

    // Issue #314: DuplicateShiftTemplateCommand unbounded while loop calling AnyAsync in each iteration
    [Fact]
    public async Task Issue314_DuplicateShiftTemplate_WhenMultipleCopiesExist_ShouldNotQueryDatabaseInWhileLoop()
    {
        using var context = CreateDbContext();
        var source = new ShiftTemplate
        {
            Id = 1,
            Name = "Shift_Alpha",
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(16, 0, 0),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.ShiftTemplates.Add(source);

        // Seed 5 existing copies
        for (int i = 1; i <= 5; i++)
        {
            context.ShiftTemplates.Add(new ShiftTemplate
            {
                Id = 10 + i,
                Name = $"Shift_Alpha_copy{i}",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(16, 0, 0),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var templateRepo = new TrackingRepository<ShiftTemplate>(context);
        var uow = new UnitOfWork(context);
        var handler = new DuplicateShiftTemplateCommandHandler(templateRepo, uow);

        var result = await handler.Handle(new DuplicateShiftTemplateCommand(1, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Shift_Alpha_copy6", result.Value!.Name);
        Assert.True(templateRepo.ReadCallCount <= 2,
            $"Issue #314: DuplicateShiftTemplateCommandHandler executed {templateRepo.ReadCallCount} read queries due to while loop querying in every iteration. Expected at most 2 queries.");
    }

    // Issue #315: GetSiteShiftsOverviewQuery over-fetching historic shifts via Include(s => s.Shifts) and memory filtering
    [Fact]
    public async Task Issue315_GetSiteShiftsOverview_ShouldPushDateRangeFilterToSql_AndNotOverFetchAllHistoricShifts()
    {
        using var context = CreateDbContext();
        var site = new Site
        {
            Id = 1,
            SiteName = "Central Site",
            Address = "Downtown"
        };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var siteRepo = new TrackingRepository<Site>(context);
        var shiftRepo = new TrackingRepository<ShiftEntity>(context);
        var empRepo = new TrackingRepository<Employee>(context);

        var handler = new GetSiteShiftsOverviewQueryHandler(siteRepo, shiftRepo, empRepo);
        var query = new GetSiteShiftsOverviewQuery(null, null, 1, 10);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);

        // Verify that Site query does not include all Shifts navigation collection
        var siteQueryExpressions = string.Join("; ", siteRepo.CapturedSpecifications.Select(e => e.ToString()));
        Assert.False(siteQueryExpressions.Contains("Shifts"),
            "Issue #315: GetSiteShiftsOverviewQueryHandler should not eagerly load all historic shifts via Include(s => s.Shifts).");

        // Verify that Shift query pushes StartTime date range filter to the database query
        var shiftQueryExpressions = string.Join("; ", shiftRepo.CapturedSpecifications.Select(e => e.ToString()));
        Assert.True(shiftQueryExpressions.Contains("StartTime"),
            "Issue #315: GetSiteShiftsOverviewQueryHandler must push the 3-day date range filter (StartTime) down to the SQL query instead of fetching all historic shifts.");
    }

    // Issue #316: GetJobsQuery loading entire Employees collection via Include(j => j.Employees) solely for counting
    [Fact]
    public async Task Issue316_GetJobsQuery_ShouldNotIncludeEntireEmployeesCollectionForCount()
    {
        using var context = CreateDbContext();
        var dept = new Department { Id = 1, Name = "Engineering" };
        context.Departments.Add(dept);
        var job = new JobRole
        {
            Id = 1,
            Title = "Software Engineer",
            DepartmentId = 1,
            IsActive = true
        };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        var jobRepo = new TrackingRepository<JobRole>(context);
        var empRepo = new TrackingRepository<Employee>(context);
        var handler = new GetJobsQueryHandler(jobRepo, empRepo);

        var result = await handler.Handle(new GetJobsQuery(new JobFilterQueryDto()), CancellationToken.None);

        Assert.NotNull(result);

        var capturedSpecifications = string.Join("; ", jobRepo.CapturedSpecifications.Select(e => e.ToString()));
        Assert.DoesNotContain("Employees", capturedSpecifications);
    }
}
