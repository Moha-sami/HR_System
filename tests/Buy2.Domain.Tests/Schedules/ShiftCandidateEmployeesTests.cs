using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.GetShiftCandidateEmployees;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Schedules;

public class ShiftCandidateEmployeesTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static GetShiftCandidateEmployeesQueryHandler CreateHandler(Buy2DbContext context)
    {
        return new GetShiftCandidateEmployeesQueryHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<AttendanceRecord>(context),
            new GenericRepository<PerformanceSubmission>(context),
            new GenericRepository<SitePreferredEmployee>(context),
            new GenericRepository<Qualification>(context)
        );
    }

    [Fact]
    public async Task Search_ByFirstName_LastName_EmployeeCode_AndId_ReturnsMatchingCandidates()
    {
        // Arrange
        using var context = CreateDbContext();
        var role = new JobRole { Id = 1, Title = "Logistics Coordinator" };
        context.JobRoles.Add(role);

        var emp1 = new Employee { Id = 10, FirstName = "Omar", LastName = "Sherif", EmployeeCode = "EMP-AAA", JobRoleId = 1, IsActive = true };
        var emp2 = new Employee { Id = 20, FirstName = "Laila", LastName = "Mourad", EmployeeCode = "EMP-BBB", JobRoleId = 1, IsActive = true };
        var emp3 = new Employee { Id = 30, FirstName = "Adel", LastName = "Emam", EmployeeCode = "EMP-CCC", JobRoleId = 1, IsActive = true };
        context.Employees.AddRange(emp1, emp2, emp3);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act & Assert 1: Search by first name
        var res1 = await handler.Handle(new GetShiftCandidateEmployeesQuery(Search: "Omar"), CancellationToken.None);
        Assert.Single(res1.Items);
        Assert.Equal(10, res1.Items[0].Id);

        // Act & Assert 2: Search by last name
        var res2 = await handler.Handle(new GetShiftCandidateEmployeesQuery(Search: "Mourad"), CancellationToken.None);
        Assert.Single(res2.Items);
        Assert.Equal(20, res2.Items[0].Id);

        // Act & Assert 3: Search by employee code
        var res3 = await handler.Handle(new GetShiftCandidateEmployeesQuery(Search: "EMP-CCC"), CancellationToken.None);
        Assert.Single(res3.Items);
        Assert.Equal(30, res3.Items[0].Id);

        // Act & Assert 4: Search by full name
        var res4 = await handler.Handle(new GetShiftCandidateEmployeesQuery(Search: "Laila Mourad"), CancellationToken.None);
        Assert.Single(res4.Items);
        Assert.Equal(20, res4.Items[0].Id);

        // Act & Assert 5: Search by ID
        var res5 = await handler.Handle(new GetShiftCandidateEmployeesQuery(Search: "10"), CancellationToken.None);
        Assert.Single(res5.Items);
        Assert.Equal(10, res5.Items[0].Id);
    }

    [Fact]
    public async Task RoleIds_MultiSelectFilter_FiltersCandidatesCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var role1 = new JobRole { Id = 10, Title = "Cashier" };
        var role2 = new JobRole { Id = 20, Title = "Stock Associate" };
        var role3 = new JobRole { Id = 30, Title = "Supervisor" };
        context.JobRoles.AddRange(role1, role2, role3);

        var emp1 = new Employee { Id = 1, FirstName = "Emp1", LastName = "Test", JobRoleId = 10, IsActive = true };
        var emp2 = new Employee { Id = 2, FirstName = "Emp2", LastName = "Test", JobRoleId = 20, IsActive = true };
        var emp3 = new Employee { Id = 3, FirstName = "Emp3", LastName = "Test", JobRoleId = 30, IsActive = true };
        context.Employees.AddRange(emp1, emp2, emp3);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act: Filter by RoleIds [10, 30]
        var result = await handler.Handle(new GetShiftCandidateEmployeesQuery(RoleIds: new List<int> { 10, 30 }), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, c => c.Id == 1 && c.JobRoleId == 10);
        Assert.Contains(result.Items, c => c.Id == 3 && c.JobRoleId == 30);
        Assert.DoesNotContain(result.Items, c => c.Id == 2);
    }

    [Fact]
    public async Task QualificationIds_MultiSelectFilter_FiltersCandidatesCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var qual1 = new Qualification { Id = 1, Name = "Forklift License" };
        var qual2 = new Qualification { Id = 2, Name = "First Aid" };
        var qual3 = new Qualification { Id = 3, Name = "Electrical Safety" };
        context.Qualifications.AddRange(qual1, qual2, qual3);

        var role1 = new JobRole { Id = 1, Title = "Driver", RequiredQualificationsJson = "[\"Forklift License\"]" };
        var role2 = new JobRole { Id = 2, Title = "Medic", RequiredQualificationsJson = "[\"First Aid\"]" };
        var role3 = new JobRole { Id = 3, Title = "Electrician", RequiredQualificationsJson = "[\"Electrical Safety\"]" };
        context.JobRoles.AddRange(role1, role2, role3);

        var emp1 = new Employee { Id = 1, FirstName = "Emp1", LastName = "A", JobRoleId = 1, IsActive = true };
        var emp2 = new Employee { Id = 2, FirstName = "Emp2", LastName = "B", JobRoleId = 2, IsActive = true };
        var emp3 = new Employee { Id = 3, FirstName = "Emp3", LastName = "C", JobRoleId = 3, IsActive = true };
        context.Employees.AddRange(emp1, emp2, emp3);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act 1: Filter by QualificationIds [1, 2]
        var result1 = await handler.Handle(new GetShiftCandidateEmployeesQuery(QualificationIds: new List<int> { 1, 2 }), CancellationToken.None);
        Assert.Equal(2, result1.TotalCount);
        Assert.Contains(result1.Items, c => c.Id == 1);
        Assert.Contains(result1.Items, c => c.Id == 2);
        Assert.DoesNotContain(result1.Items, c => c.Id == 3);

        // Act 2: Filter by QualificationIds [3]
        var result2 = await handler.Handle(new GetShiftCandidateEmployeesQuery(QualificationIds: new List<int> { 3 }), CancellationToken.None);
        Assert.Single(result2.Items);
        Assert.Equal(3, result2.Items[0].Id);
    }

    [Fact]
    public async Task WorkHoursRange_MinHours_And_MaxHours_FiltersCandidatesCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var role = new JobRole { Id = 1, Title = "Associate" };
        context.JobRoles.Add(role);

        var emp1 = new Employee { Id = 1, FirstName = "Emp1", LastName = "A", JobRoleId = 1, IsActive = true };
        var emp2 = new Employee { Id = 2, FirstName = "Emp2", LastName = "B", JobRoleId = 1, IsActive = true };
        var emp3 = new Employee { Id = 3, FirstName = "Emp3", LastName = "C", JobRoleId = 1, IsActive = true };
        context.Employees.AddRange(emp1, emp2, emp3);

        var today = DateTime.UtcNow.Date;
        context.AttendanceRecords.AddRange(
            new AttendanceRecord { EmployeeId = 1, Date = today.AddDays(-2), HoursWorked = 15m },
            new AttendanceRecord { EmployeeId = 2, Date = today.AddDays(-3), HoursWorked = 30m },
            new AttendanceRecord { EmployeeId = 3, Date = today.AddDays(-1), HoursWorked = 45m }
        );
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act & Assert 1: Range [20, 40] -> Emp 2 (30h)
        var rangeRes = await handler.Handle(new GetShiftCandidateEmployeesQuery(MinHours: 20m, MaxHours: 40m), CancellationToken.None);
        Assert.Single(rangeRes.Items);
        Assert.Equal(2, rangeRes.Items[0].Id);
        Assert.Equal(30m, rangeRes.Items[0].WeeklyCompletedHours);

        // Act & Assert 2: MinHours = 40 -> Emp 3 (45h)
        var minRes = await handler.Handle(new GetShiftCandidateEmployeesQuery(MinHours: 40m), CancellationToken.None);
        Assert.Single(minRes.Items);
        Assert.Equal(3, minRes.Items[0].Id);
        Assert.Equal(45m, minRes.Items[0].WeeklyCompletedHours);

        // Act & Assert 3: MaxHours = 20 -> Emp 1 (15h)
        var maxRes = await handler.Handle(new GetShiftCandidateEmployeesQuery(MaxHours: 20m), CancellationToken.None);
        Assert.Single(maxRes.Items);
        Assert.Equal(1, maxRes.Items[0].Id);
        Assert.Equal(15m, maxRes.Items[0].WeeklyCompletedHours);
    }

    [Fact]
    public async Task RatingTiers_MultiSelectFilter_FiltersCandidatesCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var role = new JobRole { Id = 1, Title = "General Staff" };
        context.JobRoles.Add(role);

        var emp1 = new Employee { Id = 1, FirstName = "Tier45", LastName = "A", JobRoleId = 1, IsActive = true };
        var emp2 = new Employee { Id = 2, FirstName = "Tier40", LastName = "B", JobRoleId = 1, IsActive = true };
        var emp3 = new Employee { Id = 3, FirstName = "Tier30", LastName = "C", JobRoleId = 1, IsActive = true };
        var emp4 = new Employee { Id = 4, FirstName = "Tier20", LastName = "D", JobRoleId = 1, IsActive = true };
        var emp5 = new Employee { Id = 5, FirstName = "TierLow", LastName = "E", JobRoleId = 1, IsActive = true };
        var emp6 = new Employee { Id = 6, FirstName = "Unrated", LastName = "F", JobRoleId = 1, IsActive = true };
        context.Employees.AddRange(emp1, emp2, emp3, emp4, emp5, emp6);

        context.PerformanceSubmissions.AddRange(
            new PerformanceSubmission { EmployeeId = 1, Score = 4.8m },
            new PerformanceSubmission { EmployeeId = 2, Score = 4.2m },
            new PerformanceSubmission { EmployeeId = 3, Score = 3.5m },
            new PerformanceSubmission { EmployeeId = 4, Score = 2.5m },
            new PerformanceSubmission { EmployeeId = 5, Score = 1.5m }
            // emp6 has no submission -> unrated (score = 0)
        );
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act 1: "4.5+"
        var res45 = await handler.Handle(new GetShiftCandidateEmployeesQuery(RatingTiers: new List<string> { "4.5+" }), CancellationToken.None);
        Assert.Single(res45.Items);
        Assert.Equal(1, res45.Items[0].Id);

        // Act 2: "4.0-4.49"
        var res40 = await handler.Handle(new GetShiftCandidateEmployeesQuery(RatingTiers: new List<string> { "4.0-4.49" }), CancellationToken.None);
        Assert.Single(res40.Items);
        Assert.Equal(2, res40.Items[0].Id);

        // Act 3: "below 2.0"
        var resBelow2 = await handler.Handle(new GetShiftCandidateEmployeesQuery(RatingTiers: new List<string> { "below 2.0" }), CancellationToken.None);
        Assert.Single(resBelow2.Items);
        Assert.Equal(5, resBelow2.Items[0].Id);

        // Act 4: "unrated"
        var resUnrated = await handler.Handle(new GetShiftCandidateEmployeesQuery(RatingTiers: new List<string> { "unrated" }), CancellationToken.None);
        Assert.Single(resUnrated.Items);
        Assert.Equal(6, resUnrated.Items[0].Id);

        // Act 5: Multi-select ["4.5+", "below 2.0"]
        var resMulti = await handler.Handle(new GetShiftCandidateEmployeesQuery(RatingTiers: new List<string> { "4.5+", "below 2.0" }), CancellationToken.None);
        Assert.Equal(2, resMulti.TotalCount);
        Assert.Contains(resMulti.Items, c => c.Id == 1);
        Assert.Contains(resMulti.Items, c => c.Id == 5);
    }

    [Fact]
    public async Task SitePreferredFilter_IsPreferredOnly_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var role = new JobRole { Id = 1, Title = "Tech" };
        context.JobRoles.Add(role);

        var emp1 = new Employee { Id = 1, FirstName = "Pref", LastName = "One", JobRoleId = 1, IsActive = true };
        var emp2 = new Employee { Id = 2, FirstName = "Regular", LastName = "Two", JobRoleId = 1, IsActive = true };
        context.Employees.AddRange(emp1, emp2);

        var site = new Site { Id = 5, SiteName = "Site 5" };
        context.Sites.Add(site);
        context.SitePreferredEmployees.Add(new SitePreferredEmployee { SiteId = 5, EmployeeId = 1 });
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act 1: SiteId = 5, IsPreferredOnly = null/false -> both returned with flag correctly set
        var allRes = await handler.Handle(new GetShiftCandidateEmployeesQuery(SiteId: 5), CancellationToken.None);
        Assert.Equal(2, allRes.TotalCount);
        var c1 = allRes.Items.First(c => c.Id == 1);
        var c2 = allRes.Items.First(c => c.Id == 2);
        Assert.True(c1.IsPreferredForSite);
        Assert.False(c2.IsPreferredForSite);

        // Act 2: SiteId = 5, IsPreferredOnly = true -> only emp1 returned
        var prefRes = await handler.Handle(new GetShiftCandidateEmployeesQuery(SiteId: 5, IsPreferredOnly: true), CancellationToken.None);
        Assert.Single(prefRes.Items);
        Assert.Equal(1, prefRes.Items[0].Id);
        Assert.True(prefRes.Items[0].IsPreferredForSite);
    }

    [Fact]
    public async Task RiskStatusTokens_TopPerformer_OvertimeRisk_Warning_Normal_CalculatedCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var role = new JobRole { Id = 1, Title = "Operator" };
        context.JobRoles.Add(role);

        var empTop = new Employee { Id = 1, FirstName = "Top", LastName = "Performer", JobRoleId = 1, IsActive = true };
        var empOt = new Employee { Id = 2, FirstName = "Overtime", LastName = "Risk", JobRoleId = 1, IsActive = true };
        var empWarn = new Employee { Id = 3, FirstName = "Low", LastName = "Warning", JobRoleId = 1, IsActive = true };
        var empNorm = new Employee { Id = 4, FirstName = "Normal", LastName = "Staff", JobRoleId = 1, IsActive = true };
        var empUnrated = new Employee { Id = 5, FirstName = "Unrated", LastName = "Staff", JobRoleId = 1, IsActive = true };
        context.Employees.AddRange(empTop, empOt, empWarn, empNorm, empUnrated);

        var today = DateTime.UtcNow.Date;
        // Hours: empTop: 20h, empOt: 38h, empWarn: 15h, empNorm: 20h, empUnrated: 20h
        context.AttendanceRecords.AddRange(
            new AttendanceRecord { EmployeeId = 1, Date = today.AddDays(-1), HoursWorked = 20m },
            new AttendanceRecord { EmployeeId = 2, Date = today.AddDays(-1), HoursWorked = 38m },
            new AttendanceRecord { EmployeeId = 3, Date = today.AddDays(-1), HoursWorked = 15m },
            new AttendanceRecord { EmployeeId = 4, Date = today.AddDays(-1), HoursWorked = 20m },
            new AttendanceRecord { EmployeeId = 5, Date = today.AddDays(-1), HoursWorked = 20m }
        );

        // Ratings: empTop: 4.8 (>= 4.5), empOt: 3.5, empWarn: 1.5 (< 2.0 and > 0), empNorm: 3.2, empUnrated: none (0)
        context.PerformanceSubmissions.AddRange(
            new PerformanceSubmission { EmployeeId = 1, Score = 4.8m },
            new PerformanceSubmission { EmployeeId = 2, Score = 3.5m },
            new PerformanceSubmission { EmployeeId = 3, Score = 1.5m },
            new PerformanceSubmission { EmployeeId = 4, Score = 3.2m }
        );
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetShiftCandidateEmployeesQuery(PageSize: 10), CancellationToken.None);

        // Assert
        var top = result.Items.First(c => c.Id == 1);
        Assert.Equal("TopPerformer", top.RiskStatusToken);

        var ot = result.Items.First(c => c.Id == 2);
        Assert.Equal("OvertimeRisk", ot.RiskStatusToken);

        var warn = result.Items.First(c => c.Id == 3);
        Assert.Equal("Warning", warn.RiskStatusToken);

        var norm = result.Items.First(c => c.Id == 4);
        Assert.Equal("Normal", norm.RiskStatusToken);

        var unrated = result.Items.First(c => c.Id == 5);
        Assert.Equal("Normal", unrated.RiskStatusToken);
    }

    [Fact]
    public async Task Pagination_Page_PageSize_TotalCount_BehavesCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var role = new JobRole { Id = 1, Title = "Worker" };
        context.JobRoles.Add(role);

        var employees = Enumerable.Range(1, 15).Select(i => new Employee
        {
            Id = i,
            FirstName = $"Worker{i:D2}",
            LastName = "Test",
            EmployeeCode = $"W-{i:D3}",
            JobRoleId = 1,
            IsActive = true
        }).ToList();
        context.Employees.AddRange(employees);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act 1: Page 1, PageSize 10
        var page1 = await handler.Handle(new GetShiftCandidateEmployeesQuery(Page: 1, PageSize: 10), CancellationToken.None);
        Assert.Equal(15, page1.TotalCount);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(1, page1.Page);
        Assert.Equal(10, page1.PageSize);

        // Act 2: Page 2, PageSize 10
        var page2 = await handler.Handle(new GetShiftCandidateEmployeesQuery(Page: 2, PageSize: 10), CancellationToken.None);
        Assert.Equal(15, page2.TotalCount);
        Assert.Equal(5, page2.Items.Count);
        Assert.Equal(2, page2.Page);
        Assert.Equal(10, page2.PageSize);

        // Act 3: PageSize capped at 100
        var capped = await handler.Handle(new GetShiftCandidateEmployeesQuery(Page: 1, PageSize: 200), CancellationToken.None);
        Assert.Equal(100, capped.PageSize);
        Assert.Equal(15, capped.Items.Count);
    }

    [Fact]
    public async Task ShiftCandidatesController_GetShiftCandidateEmployees_ReturnsOkWithData()
    {
        // Arrange
        var expectedDto = new PaginatedShiftCandidateEmployeesResponseDto(
            new List<ShiftCandidateEmployeeItemDto>
            {
                new ShiftCandidateEmployeeItemDto(1, "EMP-001", "Hany Shaker", "Manager", 1, 20m, 4.7m, "TopPerformer", true)
            },
            1, 1, 10
        );

        var fakeMediator = new FakeMediator((req, ct) => Task.FromResult<object?>(expectedDto));
        var controller = new ShiftCandidatesController(fakeMediator);

        // Act
        var actionResult = await controller.GetShiftCandidateEmployees(new GetShiftCandidateEmployeesQuery(), CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expectedDto, okResult.Value);
    }

    [Fact]
    public async Task OnlyActiveAndNonDeletedEmployees_AreReturned()
    {
        // Arrange
        using var context = CreateDbContext();
        var role = new JobRole { Id = 1, Title = "Staff" };
        context.JobRoles.Add(role);

        var activeEmp = new Employee { Id = 1, FirstName = "Active", LastName = "Emp", JobRoleId = 1, IsActive = true, IsDeleted = false };
        var inactiveEmp = new Employee { Id = 2, FirstName = "Inactive", LastName = "Emp", JobRoleId = 1, IsActive = false, IsDeleted = false };
        var deletedEmp = new Employee { Id = 3, FirstName = "Deleted", LastName = "Emp", JobRoleId = 1, IsActive = true, IsDeleted = true };
        context.Employees.AddRange(activeEmp, inactiveEmp, deletedEmp);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetShiftCandidateEmployeesQuery(), CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].Id);
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
