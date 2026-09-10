using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Api.Services.PointsAutomation;
using Buy2.Application.Common.Interfaces;
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

    public class InterceptingQueryable<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
    {
        private readonly IQueryable<T> _inner;
        private readonly InterceptingQueryProvider _provider;

        public InterceptingQueryable(IQueryable<T> inner, List<Expression> capturedExpressions)
        {
            _inner = inner;
            _provider = new InterceptingQueryProvider(inner.Provider, capturedExpressions);
        }

        public Type ElementType => _inner.ElementType;
        public Expression Expression => _inner.Expression;
        public IQueryProvider Provider => _provider;

        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _inner.GetEnumerator();

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (_inner is IAsyncEnumerable<T> asyncEnumerable)
            {
                return asyncEnumerable.GetAsyncEnumerator(cancellationToken);
            }
            return new InMemoryAsyncEnumerator<T>(_inner.GetEnumerator());
        }
    }

    private class InMemoryAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public InMemoryAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
        public T Current => _inner.Current;
        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
    }

    public class InterceptingQueryProvider : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;
        private readonly List<Expression> _capturedExpressions;

        public InterceptingQueryProvider(IQueryProvider inner, List<Expression> capturedExpressions)
        {
            _inner = inner;
            _capturedExpressions = capturedExpressions;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            _capturedExpressions.Add(expression);
            return _inner.CreateQuery(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            _capturedExpressions.Add(expression);
            var query = _inner.CreateQuery<TElement>(expression);
            return new InterceptingQueryable<TElement>(query, _capturedExpressions);
        }

        public object? Execute(Expression expression)
        {
            _capturedExpressions.Add(expression);
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            _capturedExpressions.Add(expression);
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            _capturedExpressions.Add(expression);
            if (_inner is IAsyncQueryProvider asyncProvider)
            {
                return asyncProvider.ExecuteAsync<TResult>(expression, cancellationToken);
            }
            return _inner.Execute<TResult>(expression);
        }
    }

    public class TrackingRepository<T> : IRepository<T> where T : class
    {
        private readonly Buy2DbContext _context;
        public int GetAllAsyncCallCount { get; private set; }
        public int QueryCallCount { get; private set; }
        public int AnyAsyncCallCount { get; private set; }
        public List<Expression> CapturedExpressions { get; } = new();

        public TrackingRepository(Buy2DbContext context)
        {
            _context = context;
        }

        public IQueryable<T> Query(bool asNoTracking = true)
        {
            QueryCallCount++;
            var q = asNoTracking ? _context.Set<T>().AsNoTracking() : _context.Set<T>().AsQueryable();
            return new InterceptingQueryable<T>(q, CapturedExpressions);
        }

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            GetAllAsyncCallCount++;
            return await _context.Set<T>().ToListAsync(cancellationToken);
        }

        public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            AnyAsyncCallCount++;
            return await _context.Set<T>().AnyAsync(predicate, cancellationToken);
        }

        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _context.AddAsync(entity, cancellationToken);
        }

        public void Update(T entity) => _context.Update(entity);
        public void Delete(T entity) => _context.Remove(entity);
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

        Assert.True(runsRepo.QueryCallCount <= 1,
            $"Issue #313: PointsAutomationDispatcher executed {runsRepo.QueryCallCount} queries across catch-up windows (N+1 query anti-pattern). Expected at most 1 query.");
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
        Assert.True(templateRepo.QueryCallCount <= 2,
            $"Issue #314: DuplicateShiftTemplateCommandHandler executed {templateRepo.QueryCallCount} queries due to while loop querying in every iteration. Expected at most 2 queries.");
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
        var siteQueryExpressions = string.Join("; ", siteRepo.CapturedExpressions.Select(e => e.ToString()));
        Assert.False(siteQueryExpressions.Contains("Shifts"),
            "Issue #315: GetSiteShiftsOverviewQueryHandler should not eagerly load all historic shifts via Include(s => s.Shifts).");

        // Verify that Shift query pushes StartTime date range filter to the database query
        var shiftQueryExpressions = string.Join("; ", shiftRepo.CapturedExpressions.Select(e => e.ToString()));
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
        var handler = new GetJobsQueryHandler(jobRepo);

        var result = await handler.Handle(new GetJobsQuery(new JobFilterQueryDto()), CancellationToken.None);

        Assert.NotNull(result);

        var capturedExpressions = string.Join("; ", jobRepo.CapturedExpressions.Select(e => e.ToString()));
        Assert.True(capturedExpressions.Contains("Select"),
            "Issue #316: GetJobsQueryHandler should project employee count at the database level using Select() projection instead of loading all JobRole entities and counting in memory.");
    }
}
