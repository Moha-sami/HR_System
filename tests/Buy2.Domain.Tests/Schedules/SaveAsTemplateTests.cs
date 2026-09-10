using System.Reflection;
using Buy2.Api.Controllers;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Schedules.SaveAsTemplate;
using Buy2.Application.Features.Sites.GetSiteShiftTemplates;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Domain.Tests.Schedules;

public class SaveAsTemplateTests
{
    private static readonly DateOnly Day = new(2026, 9, 15);

    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static SaveAsTemplateCommandHandler CreateHandler(
        Buy2DbContext context,
        IUnitOfWork? unitOfWork = null)
    {
        return new SaveAsTemplateCommandHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftTemplate>(context),
            new GenericRepository<ShiftEntity>(context),
            unitOfWork ?? new UnitOfWork(context));
    }

    private static void SeedCatalog(Buy2DbContext context, int siteId = 10)
    {
        if (context.Sites.Local.All(s => s.Id != siteId))
        {
            context.Sites.Add(new Site { Id = siteId, SiteName = $"Site {siteId}" });
        }

        foreach (var roleId in new[] { 3, 7 })
        {
            if (context.JobRoles.Local.All(r => r.Id != roleId))
            {
                context.JobRoles.Add(new JobRole { Id = roleId, Title = $"Role {roleId}" });
            }
        }

        foreach (var employeeId in new[] { 41, 42 })
        {
            if (context.Employees.Local.All(e => e.Id != employeeId))
            {
                context.Employees.Add(new Employee
                {
                    Id = employeeId,
                    FirstName = $"Emp{employeeId}",
                    LastName = "Test",
                    EmployeeCode = $"EMP-{employeeId}",
                    JobRoleId = 3,
                    IsActive = true
                });
            }
        }

        context.SaveChanges();
    }

    private static void AddShift(
        Buy2DbContext context,
        int siteId,
        DateTime startUtc,
        DateTime endUtc,
        int jobRoleId,
        int? employeeId,
        ShiftStatus status = ShiftStatus.Draft)
    {
        context.ShiftEntities.Add(new ShiftEntity
        {
            SiteId = siteId,
            JobRoleId = jobRoleId,
            EmployeeId = employeeId,
            StartTime = new DateTimeOffset(startUtc, TimeSpan.Zero),
            EndTime = new DateTimeOffset(endUtc, TimeSpan.Zero),
            IsPublished = status == ShiftStatus.Published,
            Status = status
        });
    }

    private static void SeedDay(Buy2DbContext context, int siteId = 10)
    {
        // Overlapping blocks for the same employee must stay independent.
        AddShift(context, siteId, new DateTime(2026, 9, 15, 8, 0, 0), new DateTime(2026, 9, 15, 12, 0, 0), 3, 42, ShiftStatus.Draft);
        AddShift(context, siteId, new DateTime(2026, 9, 15, 10, 0, 0), new DateTime(2026, 9, 15, 14, 0, 0), 7, 42, ShiftStatus.Published);
        AddShift(context, siteId, new DateTime(2026, 9, 15, 15, 0, 0), new DateTime(2026, 9, 15, 18, 0, 0), 3, null, ShiftStatus.PendingHrApproval);
        // Noise that must NOT be captured.
        AddShift(context, siteId, new DateTime(2026, 9, 16, 8, 0, 0), new DateTime(2026, 9, 16, 12, 0, 0), 3, 41);
        AddShift(context, siteId + 100, new DateTime(2026, 9, 15, 8, 0, 0), new DateTime(2026, 9, 15, 12, 0, 0), 3, 41);
        context.SaveChanges();
    }

    [Fact]
    public async Task HappyPath_CopiesAllShifts_WithCorrectMapping()
    {
        using var context = CreateDbContext();
        SeedCatalog(context);
        SeedDay(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new SaveAsTemplateCommand(10, Day, "Normal Monday", 99), default);

        Assert.True(result.IsSuccess);
        var dto = result.Value!;
        Assert.Equal("Normal Monday", dto.Name);
        Assert.Equal("08:00 AM", dto.StartTime);
        Assert.Equal("06:00 PM", dto.EndTime);
        Assert.Equal(3, dto.TotalBlockCount);
        Assert.Equal(3, dto.ShiftBlocks.Count);
        Assert.Contains(dto.ShiftBlocks, b =>
            b.StartTime == "08:00 AM" && b.EndTime == "12:00 PM" && b.JobRoleId == 3 && b.AssignedUserId == 42);
        Assert.Contains(dto.ShiftBlocks, b =>
            b.StartTime == "10:00 AM" && b.EndTime == "02:00 PM" && b.JobRoleId == 7 && b.AssignedUserId == 42);
        Assert.Contains(dto.ShiftBlocks, b =>
            b.StartTime == "03:00 PM" && b.EndTime == "06:00 PM" && b.JobRoleId == 3 && b.AssignedUserId == null);

        var template = await context.ShiftTemplates.Include(t => t.ShiftTemplateSites).SingleAsync();
        Assert.Single(template.ShiftTemplateSites);
        Assert.Equal(10, template.ShiftTemplateSites.Single().SiteId);
        Assert.Equal(99, template.LastUpdatedByEmployeeId);
    }

    [Fact]
    public async Task SameEmployee_InAnotherTemplate_Succeeds()
    {
        using var context = CreateDbContext();
        SeedCatalog(context);
        SeedDay(context);

        var existing = new ShiftTemplate
        {
            Name = "Existing",
            StartTime = TimeSpan.FromHours(8),
            EndTime = TimeSpan.FromHours(12)
        };
        existing.ShiftTemplateSites.Add(new ShiftTemplateSite { SiteId = 10 });
        existing.ShiftBlocks.Add(new ShiftBlock
        {
            StartTime = TimeSpan.FromHours(8),
            EndTime = TimeSpan.FromHours(12),
            JobRoleId = 3,
            EmployeeId = 42
        });
        context.ShiftTemplates.Add(existing);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var result = await handler.Handle(new SaveAsTemplateCommand(10, Day, "Second", null), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.ShiftBlocks.Count(b => b.AssignedUserId == 42));
    }

    [Fact]
    public async Task OvernightShift_SavedWithoutException()
    {
        using var context = CreateDbContext();
        SeedCatalog(context);
        AddShift(context, 10, new DateTime(2026, 9, 15, 22, 0, 0), new DateTime(2026, 9, 16, 6, 0, 0), 3, 41);
        context.SaveChanges();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new SaveAsTemplateCommand(10, Day, "Night", null), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("10:00 PM", result.Value!.StartTime);
        Assert.Equal("06:00 AM", result.Value.EndTime);
        var block = Assert.Single(result.Value.ShiftBlocks);
        Assert.Equal("10:00 PM", block.StartTime);
        Assert.Equal("06:00 AM", block.EndTime);
    }

    [Fact]
    public async Task InactiveEmployee_And_InactiveRole_CopiedAsIs()
    {
        using var context = CreateDbContext();
        SeedCatalog(context);
        var employee = await context.Employees.SingleAsync(e => e.Id == 42);
        employee.IsActive = false;
        var role = await context.JobRoles.SingleAsync(r => r.Id == 3);
        role.IsDeleted = true;
        AddShift(context, 10, new DateTime(2026, 9, 15, 8, 0, 0), new DateTime(2026, 9, 15, 12, 0, 0), 3, 42);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new SaveAsTemplateCommand(10, Day, "Snapshot", null), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, Assert.Single(result.Value!.ShiftBlocks).AssignedUserId);
    }

    [Fact]
    public async Task EmptyDay_ReturnsValidationFailure_AndCreatesNothing()
    {
        using var context = CreateDbContext();
        SeedCatalog(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new SaveAsTemplateCommand(10, Day, "Empty", null), default);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsNotFound);
        Assert.Empty(context.ShiftTemplates.Local);
    }

    [Fact]
    public async Task MissingSite_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        SeedCatalog(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new SaveAsTemplateCommand(99999, Day, "Ghost", null), default);

        Assert.True(result.IsNotFound);
        Assert.Equal("Site with ID 99999 was not found.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvalidName_ReturnsValidationFailure(string? name)
    {
        using var context = CreateDbContext();
        SeedCatalog(context);
        SeedDay(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new SaveAsTemplateCommand(10, Day, name, null), default);

        Assert.False(result.IsSuccess);
        Assert.Empty(context.ShiftTemplates.Local);
    }

    [Fact]
    public async Task Name_IsTrimmed_BeforeStorageAndResponse()
    {
        using var context = CreateDbContext();
        SeedCatalog(context);
        SeedDay(context);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new SaveAsTemplateCommand(10, Day, "  Night Shift  ", null), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Night Shift", result.Value!.Name);
        Assert.Equal("Night Shift", (await context.ShiftTemplates.SingleAsync()).Name);
    }

    [Fact]
    public async Task NameLongerThan100Chars_ReturnsValidationFailure()
    {
        using var context = CreateDbContext();
        SeedCatalog(context);
        SeedDay(context);
        var handler = CreateHandler(context);

        var tooLong = new string('N', 101);
        var rejected = await handler.Handle(new SaveAsTemplateCommand(10, Day, tooLong, null), default);
        Assert.False(rejected.IsSuccess);

        var exactly100 = new string('N', 100);
        var accepted = await handler.Handle(new SaveAsTemplateCommand(10, Day, exactly100, null), default);
        Assert.True(accepted.IsSuccess);
    }

    [Fact]
    public async Task DuplicateName_SameSiteCaseInsensitive_Rejected_OtherSiteAllowed()
    {
        using var context = CreateDbContext();
        SeedCatalog(context);
        SeedCatalog(context, siteId: 11);
        SeedDay(context);
        SeedDay(context, siteId: 11);
        var handler = CreateHandler(context);

        var first = await handler.Handle(new SaveAsTemplateCommand(10, Day, "Morning", null), default);
        Assert.True(first.IsSuccess);

        var duplicate = await handler.Handle(new SaveAsTemplateCommand(10, Day, "  morning ", null), default);
        Assert.False(duplicate.IsSuccess);
        Assert.False(duplicate.IsNotFound);

        var otherSite = await handler.Handle(new SaveAsTemplateCommand(11, Day, "Morning", null), default);
        Assert.True(otherSite.IsSuccess);
    }

    [Fact]
    public async Task Bulk500Blocks_PersistedInOneGo()
    {
        using var context = CreateDbContext();
        SeedCatalog(context);
        for (var i = 0; i < 500; i++)
        {
            var hour = i % 20;
            AddShift(context, 10,
                new DateTime(2026, 9, 15, hour, 0, 0),
                new DateTime(2026, 9, 15, hour, 0, 0).AddHours(1),
                i % 2 == 0 ? 3 : 7,
                i % 3 == 0 ? null : 41 + (i % 2));
        }
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new SaveAsTemplateCommand(10, Day, "Mega", null), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(500, result.Value!.TotalBlockCount);
    }

    [Fact]
    public async Task PersistenceFailure_Propagates_AndReturnsNoTemplate()
    {
        using var context = CreateDbContext();
        SeedCatalog(context);
        SeedDay(context);
        // InMemory has no real transactions; this stub proves a mid-save failure
        // surfaces instead of being swallowed into a fake success. Production
        // rollback is provided by ExecuteInTransactionAsync on SQL Server.
        var handler = CreateHandler(context, new FailOnSecondSaveUnitOfWork(new UnitOfWork(context)));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new SaveAsTemplateCommand(10, Day, "Broken", null), default));
        Assert.Equal("simulated persistence failure", exception.Message);
    }

    [Fact]
    public async Task NonDuplicateDbFailure_RethrowsInsteadOfFakeDuplicate()
    {
        using var context = CreateDbContext();
        SeedCatalog(context);
        SeedDay(context);
        var handler = CreateHandler(context, new DbFailingUnitOfWork(new UnitOfWork(context)));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => handler.Handle(new SaveAsTemplateCommand(10, Day, "Broken", null), default));
    }

    [Fact]
    public void ControllerAction_HasExpectedRouteRolesAndResponse()
    {
        var method = typeof(GetSitesController).GetMethod(nameof(GetSitesController.SaveAsTemplate));
        Assert.NotNull(method);

        var route = method.GetCustomAttributes(typeof(HttpPostAttribute), false)
            .OfType<HttpPostAttribute>().Single();
        Assert.Equal("{siteId}/dates/{date}/save-as-template", route.Template);

        var authorize = method.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>().Single();
        Assert.Equal("Admin,Manager,OperationsManager", authorize.Roles);

        var responses = method.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), false)
            .OfType<ProducesResponseTypeAttribute>().ToList();
        Assert.Contains(responses, p => p.StatusCode == 201 && p.Type == typeof(SiteShiftTemplateDto));
    }

    private sealed class FailOnSecondSaveUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _inner;
        private int _saves;

        public FailOnSecondSaveUnitOfWork(IUnitOfWork inner) => _inner = inner;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            _saves++;
            if (_saves >= 2)
            {
                throw new InvalidOperationException("simulated persistence failure");
            }

            return _inner.SaveChangesAsync(cancellationToken);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            _inner.BeginTransactionAsync(cancellationToken);

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) =>
            _inner.CommitTransactionAsync(cancellationToken);

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) =>
            _inner.RollbackTransactionAsync(cancellationToken);

        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default) =>
            _inner.ExecuteInTransactionAsync(operation, cancellationToken);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default) =>
            _inner.ExecuteInTransactionAsync(operation, cancellationToken);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<Task<TResult>> operation,
            System.Data.IsolationLevel isolationLevel,
            CancellationToken cancellationToken = default) =>
            _inner.ExecuteInTransactionAsync(operation, isolationLevel, cancellationToken);
    }

    private sealed class DbFailingUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _inner;

        public DbFailingUnitOfWork(IUnitOfWork inner) => _inner = inner;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("simulated db failure", new Exception("inner"));

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            _inner.BeginTransactionAsync(cancellationToken);

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) =>
            _inner.CommitTransactionAsync(cancellationToken);

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) =>
            _inner.RollbackTransactionAsync(cancellationToken);

        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default) =>
            _inner.ExecuteInTransactionAsync(operation, cancellationToken);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default) =>
            _inner.ExecuteInTransactionAsync(operation, cancellationToken);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<Task<TResult>> operation,
            System.Data.IsolationLevel isolationLevel,
            CancellationToken cancellationToken = default) =>
            _inner.ExecuteInTransactionAsync(operation, isolationLevel, cancellationToken);
    }
}
