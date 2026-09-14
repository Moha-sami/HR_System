using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Application.Features.Employees.BulkOnboard;
using Buy2.Application.Features.Jobs.ExportJobs;
using Buy2.Application.Features.Points.Automation;
using Buy2.Application.Features.Points.Automation.Evaluators;
using Buy2.Application.Features.Points.Automation.SaveAutomationSettings;
using Buy2.Application.Features.ShiftTemplates.CreateShiftTemplate;
using Buy2.Application.Features.ShiftTemplates.DTOs;
using Buy2.Application.Features.Sites.GetSiteShiftsOverview;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Buy2.Domain.Tests.Performance;

public class DatabaseAndBatchPerformanceReproductionTests
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
        private readonly Buy2DbContext _context;
        public int GetAllAsyncCallCount { get; private set; }
        public int QueryCallCount { get; private set; }
        public int AnyAsyncCallCount { get; private set; }
        public int AddAsyncCallCount { get; private set; }

        public TrackingRepository(Buy2DbContext context)
        {
            _context = context;
        }

        public IQueryable<T> Query(bool asNoTracking = true)
        {
            QueryCallCount++;
            return asNoTracking ? _context.Set<T>().AsNoTracking() : _context.Set<T>().AsQueryable();
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
            AddAsyncCallCount++;
            await _context.AddAsync(entity, cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            await _context.AddRangeAsync(entities, cancellationToken);
        }

        public void Update(T entity) => _context.Update(entity);
        public void Delete(T entity) => _context.Remove(entity);
    }

    private class FakeEvaluator : IAutomationEvaluator
    {
        public AutomationCategory Category => AutomationCategory.Tasks;
        public Task<(int TotalPoints, List<RuleEvaluationDetailDto> RuleEvaluations)> EvaluateAsync(
            EvaluationContext context,
            List<PointsAutomationSetting> settings,
            CancellationToken cancellationToken)
        {
            var evaluations = new List<RuleEvaluationDetailDto>
            {
                new("Reward", "CompletedTasks", 5, "1-10", 10, null, null, "Tasks")
            };
            return Task.FromResult((10, evaluations));
        }
    }

    // Issue #331: GetSiteShiftsOverviewQuery in-memory shift date filtering -> push date filter to database
    [Fact]
    public async Task Issue331_GetSiteShiftsOverviewQuery_ShouldNotEagerlyLoadHistoricShiftsIntoMemory()
    {
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Main Site", Address = "123 St" };
        var pastShift = new ShiftEntity
        {
            Id = 1,
            SiteId = 1,
            StartTime = DateTimeOffset.UtcNow.AddDays(-100),
            EndTime = DateTimeOffset.UtcNow.AddDays(-100).AddHours(8),
            IsPublished = true
        };
        var futureShift = new ShiftEntity
        {
            Id = 2,
            SiteId = 1,
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(8),
            IsPublished = true
        };
        context.Sites.Add(site);
        context.ShiftEntities.AddRange(pastShift, futureShift);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        site.Shifts.Clear();

        var siteRepo = new GenericRepository<Site>(context);
        var handler = new Buy2.Application.Features.Sites.GetSiteShiftsOverview.GetSiteShiftsOverviewQueryHandler(siteRepo);

        var result = await handler.Handle(new Buy2.Application.Features.Sites.GetSiteShiftsOverview.GetSiteShiftsOverviewQuery(), CancellationToken.None);

        Assert.NotNull(result);

        // Current code does Include(s => s.Shifts) which eagerly loads all historic shifts into site.Shifts.
        // When pushed to database, site.Shifts should not contain historic shifts outside the 3-day window.
        Assert.False(site.Shifts.Any(s => s.StartTime < DateTimeOffset.UtcNow.Date),
            "Issue #331: GetSiteShiftsOverviewQueryHandler eagerly loaded historic shifts from 100 days ago into memory via Include(s => s.Shifts) instead of filtering at database level.");
    }

    // Issue #332: ExportJobsQuery full eager loading of Employees and Department -> optimize with database projection/lightweight queries
    [Fact]
    public async Task Issue332_ExportJobsQuery_ShouldNotEagerlyLoadEntireEmployeesCollectionIntoMemory()
    {
        using var context = CreateDbContext();
        var dept = new Department { Id = 1, Name = "HR" };
        var job = new JobRole { Id = 1, Title = "Recruiter", DepartmentId = 1, IsActive = true };
        context.Departments.Add(dept);
        context.JobRoles.Add(job);

        for (int i = 1; i <= 5; i++)
        {
            context.Employees.Add(new Employee
            {
                Id = i,
                FirstName = $"Emp{i}",
                LastName = "Test",
                Email = $"emp{i}@test.com",
                EmployeeCode = $"E00{i}",
                JobRoleId = 1
            });
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        job.Employees.Clear();

        var jobRepo = new GenericRepository<JobRole>(context);
        var handler = new ExportJobsQueryHandler(jobRepo);

        var result = await handler.Handle(new ExportJobsQuery(), CancellationToken.None);

        Assert.NotNull(result);

        // Current code does Include(j => j.Employees) which eagerly loads the entire Employees entity collection into memory just to count them.
        Assert.False(job.Employees.Any(),
            "Issue #332: ExportJobsQueryHandler eagerly loaded full Employee entity objects into memory via Include(j => j.Employees) instead of using database count projection.");
    }

    // Issue #333: PointsAutomationRunner.cs:240 N+1 inserts in loop -> batch insert with AddRangeAsync
    [Fact]
    public async Task Issue333_PointsAutomationRunner_ShouldBatchInsertTransactions_NotLoopAddAsync()
    {
        using var context = CreateDbContext();
        var setting = new PointsAutomationSetting
        {
            Category = AutomationCategory.Tasks,
            SubCategory = "CompletedTasks",
            AutomationPeriod = AutomationPeriod.Daily,
            IsEnabled = true
        };
        context.PointsAutomationSettings.Add(setting);
        await context.SaveChangesAsync();

        var range = new PointsAutomationRange
        {
            AutomationSettingId = setting.Id,
            RangeType = "Reward",
            FromValue = 1,
            ToValue = 10,
            PointsValue = 10
        };
        context.PointsAutomationRanges.Add(range);
        await context.SaveChangesAsync();

        for (int i = 1; i <= 3; i++)
        {
            context.Employees.Add(new Employee
            {
                FirstName = $"Emp{i}",
                LastName = "Test",
                Email = $"emp{i}@test.com",
                EmployeeCode = $"E00{i}",
                JobRoleId = 1,
                IsActive = true
            });
        }
        await context.SaveChangesAsync();

        var settingsRepo = new GenericRepository<PointsAutomationSetting>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var attRepo = new GenericRepository<AttendanceRecord>(context);
        var taskRepo = new GenericRepository<EmployeeTask>(context);
        var perfRepo = new GenericRepository<PerformanceSubmission>(context);
        var transRepo = new TrackingRepository<PointsTransaction>(context);
        var runRepo = new GenericRepository<PointsAutomationRun>(context);
        var uow = new UnitOfWork(context);
        var evaluators = new IAutomationEvaluator[] { new FakeEvaluator() };
        var logger = NullLogger<PointsAutomationRunner>.Instance;

        var runner = new PointsAutomationRunner(
            settingsRepo, empRepo, attRepo, taskRepo, perfRepo, transRepo, runRepo, uow, evaluators, logger);

        var result = await runner.RunAsync(
            AutomationCategory.Tasks,
            AutomationPeriod.Daily,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(transRepo.AddAsyncCallCount == 0,
            $"Issue #333: PointsAutomationRunner executed {transRepo.AddAsyncCallCount} individual AddAsync calls in a loop. Expected batch AddRangeAsync insertion.");
    }

    // Issue #334: BulkOnboardCommand.cs:272 N+1 inserts in loop -> batch insert with AddRangeAsync
    [Fact]
    public async Task Issue334_BulkOnboardCommand_ShouldBatchInsertEmployees_NotLoopAddAsync()
    {
        using var context = CreateDbContext();
        var role = new Role { Id = 1, Name = "Staff", IsActive = true };
        var jobRole = new JobRole { Id = 1, Title = "Engineer", IsActive = true };
        var site = new Site { Id = 1, SiteName = "Site A" };
        context.Roles.Add(role);
        context.JobRoles.Add(jobRole);
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var empRepo = new TrackingRepository<Employee>(context);
        var roleRepo = new TrackingRepository<Role>(context);
        var jobRoleRepo = new TrackingRepository<JobRole>(context);
        var siteRepo = new TrackingRepository<Site>(context);
        var uow = new UnitOfWork(context);

        var handler = new BulkOnboardCommandHandler(empRepo, roleRepo, jobRoleRepo, siteRepo, uow);
        var items = new List<BulkOnboardEmployeeItemDto>
        {
            new() { FirstName = "Alice", LastName = "Smith", Email = "alice@test.com", RoleId = 1, JobRoleId = 1, SiteId = 1 },
            new() { FirstName = "Bob", LastName = "Jones", Email = "bob@test.com", RoleId = 1, JobRoleId = 1, SiteId = 1 },
            new() { FirstName = "Charlie", LastName = "Brown", Email = "charlie@test.com", RoleId = 1, JobRoleId = 1, SiteId = 1 }
        };

        var result = await handler.Handle(new BulkOnboardCommand(items), CancellationToken.None);

        Assert.Equal(3, result.CreatedCount);
        Assert.True(empRepo.AddAsyncCallCount == 0,
            $"Issue #334: BulkOnboardCommandHandler executed {empRepo.AddAsyncCallCount} individual AddAsync calls in a loop. Expected batch AddRangeAsync insertion.");
    }

    // Issue #335: CreateShiftTemplateCommand.cs:126,138 N+1 inserts in loops -> batch insert with AddRangeAsync
    [Fact]
    public async Task Issue335_CreateShiftTemplateCommand_ShouldBatchInsertShiftBlocksAndSites_NotLoopAddAsync()
    {
        using var context = CreateDbContext();
        var site1 = new Site { Id = 1, SiteName = "Site 1" };
        var site2 = new Site { Id = 2, SiteName = "Site 2" };
        var jobRole = new JobRole { Id = 1, Title = "Cashier" };
        var emp = new Employee { Id = 1, FirstName = "John", LastName = "Doe", Email = "j@d.com", EmployeeCode = "E1" };
        context.Sites.AddRange(site1, site2);
        context.JobRoles.Add(jobRole);
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var templateRepo = new TrackingRepository<ShiftTemplate>(context);
        var siteTemplateRepo = new TrackingRepository<ShiftTemplateSite>(context);
        var blockRepo = new TrackingRepository<ShiftBlock>(context);
        var siteRepo = new TrackingRepository<Site>(context);
        var jobRoleRepo = new TrackingRepository<JobRole>(context);
        var empRepo = new TrackingRepository<Employee>(context);
        var uow = new UnitOfWork(context);

        var handler = new CreateShiftTemplateCommandHandler(
            templateRepo, siteTemplateRepo, blockRepo, siteRepo, jobRoleRepo, empRepo, uow);

        var dto = new CreateShiftTemplateDto(
            Name: "Standard Shift",
            SiteIds: new List<int> { 1, 2 },
            StartTime: "09:00 AM",
            EndTime: "05:00 PM",
            ShiftBlocks: new List<CreateShiftTemplateBlockDto>
            {
                new("09:00 AM", "01:00 PM", 1, 1),
                new("01:00 PM", "05:00 PM", 1, 1)
            }
        );

        var result = await handler.Handle(new CreateShiftTemplateCommand(dto, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(siteTemplateRepo.AddAsyncCallCount == 0,
            $"Issue #335: CreateShiftTemplateCommandHandler executed {siteTemplateRepo.AddAsyncCallCount} individual AddAsync calls for ShiftTemplateSites. Expected batch AddRangeAsync.");
        Assert.True(blockRepo.AddAsyncCallCount == 0,
            $"Issue #335: CreateShiftTemplateCommandHandler executed {blockRepo.AddAsyncCallCount} individual AddAsync calls for ShiftBlocks. Expected batch AddRangeAsync.");
    }

    // Issue #336: SaveAutomationSettingsCommandHandler.cs:155,170 nested N+1 inserts -> batch insert with AddRangeAsync
    [Fact]
    public async Task Issue336_SaveAutomationSettingsCommand_ShouldBatchInsertRanges_NotLoopAddAsync()
    {
        using var context = CreateDbContext();
        var setting = new PointsAutomationSetting
        {
            Id = 1,
            Category = AutomationCategory.TimeAndAttendance,
            SubCategory = "Tardiness",
            AutomationPeriod = AutomationPeriod.Daily,
            IsEnabled = true,
            Ranges = new List<PointsAutomationRange>()
        };
        context.PointsAutomationSettings.Add(setting);
        await context.SaveChangesAsync();

        var settingRepo = new TrackingRepository<PointsAutomationSetting>(context);
        var rangeRepo = new TrackingRepository<PointsAutomationRange>(context);
        var uow = new UnitOfWork(context);

        var handler = new SaveAutomationSettingsCommandHandler(settingRepo, rangeRepo, uow);
        var requestDto = new SaveAutomationSettingsDto(
            AutomationPeriod: "Daily",
            Settings: new List<AutomationSettingCategoryDto>
            {
                new(
                    Id: 1,
                    Category: "TimeAndAttendance",
                    SubCategory: "Tardiness",
                    IsEnabled: true,
                    Ranges: new List<AutomationRangeDto>
                    {
                        new(null, "Deduction", 1, 15, null, 5),
                        new(null, "Deduction", 16, 30, null, 10),
                        new(null, "Deduction", 31, 60, null, 20)
                    }
                )
            }
        );

        var result = await handler.Handle(new SaveAutomationSettingsCommand(requestDto), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(rangeRepo.AddAsyncCallCount == 0,
            $"Issue #336: SaveAutomationSettingsCommandHandler executed {rangeRepo.AddAsyncCallCount} individual AddAsync calls for ranges. Expected batch AddRangeAsync.");
    }

    // Issue #337: Missing composite index on Shift(IsPublished, EmployeeId, StartTime)
    [Fact]
    public void Issue337_ShiftEntity_ShouldHaveCompositeIndex_On_IsPublished_EmployeeId_StartTime()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(ShiftEntity));
        Assert.NotNull(entityType);

        var indexes = entityType.GetIndexes();
        var compositeIndex = indexes.FirstOrDefault(idx =>
        {
            var props = idx.Properties.Select(p => p.Name).ToList();
            return props.SequenceEqual(new[] { "IsPublished", "EmployeeId", "StartTime" });
        });

        Assert.True(compositeIndex != null,
            "Issue #337: Missing composite index on ShiftEntity for properties (IsPublished, EmployeeId, StartTime).");
    }

    // Issue #338: Missing composite index on AttendanceRecord(EmployeeId, Date)
    [Fact]
    public void Issue338_AttendanceRecord_ShouldHaveCompositeIndex_On_EmployeeId_Date()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(AttendanceRecord));
        Assert.NotNull(entityType);

        var indexes = entityType.GetIndexes();
        var compositeIndex = indexes.FirstOrDefault(idx =>
        {
            var props = idx.Properties.Select(p => p.Name).ToList();
            return props.SequenceEqual(new[] { "EmployeeId", "Date" });
        });

        Assert.True(compositeIndex != null,
            "Issue #338: Missing composite index on AttendanceRecord for properties (EmployeeId, Date).");
    }

    // Issue #339: Missing composite index on EmployeeTask(EmployeeId, DueDate)
    [Fact]
    public void Issue339_EmployeeTask_ShouldHaveCompositeIndex_On_EmployeeId_DueDate()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(EmployeeTask));
        Assert.NotNull(entityType);

        var indexes = entityType.GetIndexes();
        var compositeIndex = indexes.FirstOrDefault(idx =>
        {
            var props = idx.Properties.Select(p => p.Name).ToList();
            return props.SequenceEqual(new[] { "EmployeeId", "DueDate" });
        });

        Assert.True(compositeIndex != null,
            "Issue #339: Missing composite index on EmployeeTask for properties (EmployeeId, DueDate).");
    }

    // Issue #340: Missing composite index on PerformanceSubmission(EmployeeId, SubmissionDate)
    [Fact]
    public void Issue340_PerformanceSubmission_ShouldHaveCompositeIndex_On_EmployeeId_SubmissionDate()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(PerformanceSubmission));
        Assert.NotNull(entityType);

        var indexes = entityType.GetIndexes();
        var compositeIndex = indexes.FirstOrDefault(idx =>
        {
            var props = idx.Properties.Select(p => p.Name).ToList();
            return props.SequenceEqual(new[] { "EmployeeId", "SubmissionDate" });
        });

        Assert.True(compositeIndex != null,
            "Issue #340: Missing composite index on PerformanceSubmission for properties (EmployeeId, SubmissionDate).");
    }

    // Issue #341: Missing composite index on PointsAutomationRun(Status, Category, PeriodEnd DESC)
    [Fact]
    public void Issue341_PointsAutomationRun_ShouldHaveCompositeIndex_On_Status_Category_PeriodEnd()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(PointsAutomationRun));
        Assert.NotNull(entityType);

        var indexes = entityType.GetIndexes();
        var compositeIndex = indexes.FirstOrDefault(idx =>
        {
            var props = idx.Properties.Select(p => p.Name).ToList();
            return props.SequenceEqual(new[] { "Status", "Category", "PeriodEnd" });
        });

        Assert.True(compositeIndex != null,
            "Issue #341: Missing composite index on PointsAutomationRun for properties (Status, Category, PeriodEnd).");
    }
}
