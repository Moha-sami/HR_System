using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.Candidates;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Schedules;

public class ShiftCandidatesTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task CandidateSearch_ReturnsOnlyActiveAndNonDeletedEmployees()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRole = new JobRole { Id = 1, Title = "Warehouse Associate" };
        context.JobRoles.Add(jobRole);

        var activeEmp = new Employee
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            EmployeeCode = "EMP001",
            JobRoleId = 1,
            IsActive = true,
            IsDeleted = false
        };

        var inactiveEmp = new Employee
        {
            Id = 2,
            FirstName = "Jane",
            LastName = "Smith",
            EmployeeCode = "EMP002",
            JobRoleId = 1,
            IsActive = false,
            IsDeleted = false
        };

        var deletedEmp = new Employee
        {
            Id = 3,
            FirstName = "Bob",
            LastName = "Brown",
            EmployeeCode = "EMP003",
            JobRoleId = 1,
            IsActive = true,
            IsDeleted = true
        };

        context.Employees.AddRange(activeEmp, inactiveEmp, deletedEmp);
        await context.SaveChangesAsync();

        var handler = new GetShiftCandidatesQueryHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<AttendanceRecord>(context),
            new GenericRepository<PerformanceSubmission>(context),
            new GenericRepository<SitePreferredEmployee>(context)
        );

        // Act
        var result = await handler.Handle(new GetShiftCandidatesQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalCount);
        var candidate = Assert.Single(result.Items);
        Assert.Equal(1, candidate.Id);
        Assert.Equal("John Doe", candidate.FullName);
    }

    [Fact]
    public async Task CandidateSearch_FilterByNameAndCode_ReturnsMatchingCandidates()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRole = new JobRole { Id = 1, Title = "Forklift Driver" };
        context.JobRoles.Add(jobRole);

        var emp1 = new Employee { Id = 10, FirstName = "Ahmed", LastName = "Hassan", EmployeeCode = "DRV-100", JobRoleId = 1, IsActive = true };
        var emp2 = new Employee { Id = 11, FirstName = "Mohamed", LastName = "Ali", EmployeeCode = "DRV-200", JobRoleId = 1, IsActive = true };
        var emp3 = new Employee { Id = 12, FirstName = "Sara", LastName = "Ibrahim", EmployeeCode = "DRV-300", JobRoleId = 1, IsActive = true };

        context.Employees.AddRange(emp1, emp2, emp3);
        await context.SaveChangesAsync();

        var handler = new GetShiftCandidatesQueryHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<AttendanceRecord>(context),
            new GenericRepository<PerformanceSubmission>(context),
            new GenericRepository<SitePreferredEmployee>(context)
        );

        // Act & Assert 1: Search by First Name
        var res1 = await handler.Handle(new GetShiftCandidatesQuery(Search: "Ahmed"), CancellationToken.None);
        Assert.Single(res1.Items);
        Assert.Equal(10, res1.Items[0].Id);

        // Act & Assert 2: Search by Last Name
        var res2 = await handler.Handle(new GetShiftCandidatesQuery(Search: "Ali"), CancellationToken.None);
        Assert.Single(res2.Items);
        Assert.Equal(11, res2.Items[0].Id);

        // Act & Assert 3: Search by Employee Code
        var res3 = await handler.Handle(new GetShiftCandidatesQuery(Search: "DRV-300"), CancellationToken.None);
        Assert.Single(res3.Items);
        Assert.Equal(12, res3.Items[0].Id);

        // Act & Assert 4: Search by Full Name
        var res4 = await handler.Handle(new GetShiftCandidatesQuery(Search: "Sara Ibrahim"), CancellationToken.None);
        Assert.Single(res4.Items);
        Assert.Equal(12, res4.Items[0].Id);
    }

    [Fact]
    public async Task CandidateSearch_JobRoleMultiSelectFilter_ReturnsMatchingRoles()
    {
        // Arrange
        using var context = CreateDbContext();
        var role1 = new JobRole { Id = 1, Title = "Cashier" };
        var role2 = new JobRole { Id = 2, Title = "Stock Associate" };
        var role3 = new JobRole { Id = 3, Title = "Shift Supervisor" };
        context.JobRoles.AddRange(role1, role2, role3);

        var emp1 = new Employee { Id = 1, FirstName = "Emp1", LastName = "Test", JobRoleId = 1, IsActive = true };
        var emp2 = new Employee { Id = 2, FirstName = "Emp2", LastName = "Test", JobRoleId = 2, IsActive = true };
        var emp3 = new Employee { Id = 3, FirstName = "Emp3", LastName = "Test", JobRoleId = 3, IsActive = true };

        context.Employees.AddRange(emp1, emp2, emp3);
        await context.SaveChangesAsync();

        var handler = new GetShiftCandidatesQueryHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<AttendanceRecord>(context),
            new GenericRepository<PerformanceSubmission>(context),
            new GenericRepository<SitePreferredEmployee>(context)
        );

        // Act: Filter by JobRoleIds [1, 3]
        var result = await handler.Handle(new GetShiftCandidatesQuery(JobRoleIds: new List<int> { 1, 3 }), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, c => c.Id == 1 && c.JobRoleId == 1);
        Assert.Contains(result.Items, c => c.Id == 3 && c.JobRoleId == 3);
        Assert.DoesNotContain(result.Items, c => c.Id == 2);
    }

    [Fact]
    public async Task CandidateSearch_QualificationsFilter_MatchesCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var roleWithCPR = new JobRole
        {
            Id = 1,
            Title = "Lifeguard",
            RequiredQualificationsJson = "[\"CPR\", \"First Aid\", \"Swimming\"]"
        };
        var roleWithWelding = new JobRole
        {
            Id = 2,
            Title = "Welder",
            RequiredQualificationsJson = "[\"Welding Cert\", \"Metal Fabrication\"]"
        };
        context.JobRoles.AddRange(roleWithCPR, roleWithWelding);

        var emp1 = new Employee { Id = 1, FirstName = "Lifeguard", LastName = "One", JobRoleId = 1, IsActive = true };
        var emp2 = new Employee { Id = 2, FirstName = "Welder", LastName = "One", JobRoleId = 2, IsActive = true };
        context.Employees.AddRange(emp1, emp2);
        await context.SaveChangesAsync();

        var handler = new GetShiftCandidatesQueryHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<AttendanceRecord>(context),
            new GenericRepository<PerformanceSubmission>(context),
            new GenericRepository<SitePreferredEmployee>(context)
        );

        // Act 1: Search for "CPR"
        var cprResult = await handler.Handle(new GetShiftCandidatesQuery(Qualifications: new List<string> { "CPR" }), CancellationToken.None);
        Assert.Single(cprResult.Items);
        Assert.Equal(1, cprResult.Items[0].Id);

        // Act 2: Search for "Welding Cert"
        var weldResult = await handler.Handle(new GetShiftCandidatesQuery(Qualifications: new List<string> { "Welding Cert" }), CancellationToken.None);
        Assert.Single(weldResult.Items);
        Assert.Equal(2, weldResult.Items[0].Id);

        // Act 3: Search for non-matching qualification
        var emptyResult = await handler.Handle(new GetShiftCandidatesQuery(Qualifications: new List<string> { "Pilot License" }), CancellationToken.None);
        Assert.Empty(emptyResult.Items);
    }

    [Fact]
    public async Task CandidateSearch_WeeklyHoursAndRatingRangeFilters_AppliesFiltersCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRole = new JobRole { Id = 1, Title = "General Staff" };
        context.JobRoles.Add(jobRole);

        var emp1 = new Employee { Id = 1, FirstName = "Emp1", LastName = "A", JobRoleId = 1, IsActive = true };
        var emp2 = new Employee { Id = 2, FirstName = "Emp2", LastName = "B", JobRoleId = 1, IsActive = true };
        var emp3 = new Employee { Id = 3, FirstName = "Emp3", LastName = "C", JobRoleId = 1, IsActive = true };
        context.Employees.AddRange(emp1, emp2, emp3);

        // Attendance records in current week (last 7 days)
        var today = DateTime.UtcNow.Date;
        context.AttendanceRecords.AddRange(
            new AttendanceRecord { EmployeeId = 1, Date = today.AddDays(-2), HoursWorked = 30m },
            new AttendanceRecord { EmployeeId = 2, Date = today.AddDays(-3), HoursWorked = 10m },
            new AttendanceRecord { EmployeeId = 3, Date = today.AddDays(-1), HoursWorked = 50m }
        );

        // Performance submissions
        context.PerformanceSubmissions.AddRange(
            new PerformanceSubmission { EmployeeId = 1, Score = 4.8m },
            new PerformanceSubmission { EmployeeId = 2, Score = 3.2m },
            new PerformanceSubmission { EmployeeId = 3, Score = 4.0m }
        );

        await context.SaveChangesAsync();

        var handler = new GetShiftCandidatesQueryHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<AttendanceRecord>(context),
            new GenericRepository<PerformanceSubmission>(context),
            new GenericRepository<SitePreferredEmployee>(context)
        );

        // Act & Assert 1: Hours filter [20 - 40]
        var hoursResult = await handler.Handle(new GetShiftCandidatesQuery(MinHours: 20m, MaxHours: 40m), CancellationToken.None);
        Assert.Single(hoursResult.Items);
        Assert.Equal(1, hoursResult.Items[0].Id);
        Assert.Equal(30m, hoursResult.Items[0].CurrentWeeklyHours);

        // Act & Assert 2: Rating filter >= 4.5
        var ratingResult = await handler.Handle(new GetShiftCandidatesQuery(MinRating: 4.5m), CancellationToken.None);
        Assert.Single(ratingResult.Items);
        Assert.Equal(1, ratingResult.Items[0].Id);
        Assert.Equal(4.8m, ratingResult.Items[0].RatingScore);

        // Act & Assert 3: Rating filter <= 3.5
        var lowRatingResult = await handler.Handle(new GetShiftCandidatesQuery(MaxRating: 3.5m), CancellationToken.None);
        Assert.Single(lowRatingResult.Items);
        Assert.Equal(2, lowRatingResult.Items[0].Id);
        Assert.Equal(3.2m, lowRatingResult.Items[0].RatingScore);
    }

    [Fact]
    public async Task CandidateSearch_PreferredSiteFilter_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRole = new JobRole { Id = 1, Title = "Technician" };
        context.JobRoles.Add(jobRole);

        var emp1 = new Employee { Id = 1, FirstName = "PreferredEmp", LastName = "One", JobRoleId = 1, IsActive = true };
        var emp2 = new Employee { Id = 2, FirstName = "RegularEmp", LastName = "Two", JobRoleId = 1, IsActive = true };
        context.Employees.AddRange(emp1, emp2);

        var site = new Site { Id = 10, SiteName = "Site 10" };
        context.Sites.Add(site);

        context.SitePreferredEmployees.Add(new SitePreferredEmployee { SiteId = 10, EmployeeId = 1 });
        await context.SaveChangesAsync();

        var handler = new GetShiftCandidatesQueryHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<AttendanceRecord>(context),
            new GenericRepository<PerformanceSubmission>(context),
            new GenericRepository<SitePreferredEmployee>(context)
        );

        // Act 1: SiteId = 10 without IsPreferredOnly -> both returned, emp1 is preferred
        var allForSite = await handler.Handle(new GetShiftCandidatesQuery(SiteId: 10), CancellationToken.None);
        Assert.Equal(2, allForSite.TotalCount);
        var card1 = allForSite.Items.First(c => c.Id == 1);
        var card2 = allForSite.Items.First(c => c.Id == 2);
        Assert.True(card1.IsPreferredForSite);
        Assert.False(card2.IsPreferredForSite);

        // Act 2: SiteId = 10 with IsPreferredOnly = true -> only emp1 returned
        var preferredOnly = await handler.Handle(new GetShiftCandidatesQuery(SiteId: 10, IsPreferredOnly: true), CancellationToken.None);
        Assert.Single(preferredOnly.Items);
        Assert.Equal(1, preferredOnly.Items[0].Id);
        Assert.True(preferredOnly.Items[0].IsPreferredForSite);
    }

    [Fact]
    public async Task GetShiftCandidatePreview_CalculatesAllFieldsAccurately()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRole = new JobRole
        {
            Id = 5,
            Title = "Senior Barista",
            RequiredQualificationsJson = "[\"Food Hygiene\", \"Latte Art\"]"
        };
        context.JobRoles.Add(jobRole);

        var joinDate = new DateTime(2023, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        var employee = new Employee
        {
            Id = 42,
            EmployeeCode = "EMP-0042",
            FirstName = "Youssef",
            LastName = "Adel",
            ProfilePhotoUrl = "https://example.com/photo.jpg",
            JobRoleId = 5,
            JoinDate = joinDate,
            IsActive = true
        };
        context.Employees.Add(employee);

        var payroll = new PayrollProfile
        {
            EmployeeId = 42,
            SalaryType = "Hourly",
            PaymentAmount = 25.50m
        };
        context.PayrollProfiles.Add(payroll);

        var site = new Site { Id = 3, SiteName = "Cairo Branch" };
        context.Sites.Add(site);
        context.SitePreferredEmployees.Add(new SitePreferredEmployee { SiteId = 3, EmployeeId = 42 });

        context.AttendanceRecords.AddRange(
            new AttendanceRecord { EmployeeId = 42, HoursWorked = 80.5m, Date = DateTime.UtcNow.AddMonths(-2) },
            new AttendanceRecord { EmployeeId = 42, HoursWorked = 40.0m, Date = DateTime.UtcNow.AddMonths(-1) }
        );

        context.PerformanceSubmissions.AddRange(
            new PerformanceSubmission { EmployeeId = 42, Score = 4.0m },
            new PerformanceSubmission { EmployeeId = 42, Score = 5.0m }
        );

        await context.SaveChangesAsync();

        var handler = new GetShiftCandidatePreviewQueryHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<AttendanceRecord>(context),
            new GenericRepository<PerformanceSubmission>(context),
            new GenericRepository<SitePreferredEmployee>(context)
        );

        // Act
        var preview = await handler.Handle(new GetShiftCandidatePreviewQuery(42, SiteId: 3), CancellationToken.None);

        // Assert
        Assert.NotNull(preview);
        Assert.Equal(42, preview.Id);
        Assert.Equal("EMP-0042", preview.EmployeeCode);
        Assert.Equal("Youssef Adel", preview.FullName);
        Assert.Equal("https://example.com/photo.jpg", preview.AvatarUrl);
        Assert.Equal("Senior Barista", preview.JobTitle);
        Assert.Equal(joinDate, preview.JoinDate);
        Assert.Equal(25.50m, preview.HourlyRate);
        Assert.Equal(4.5m, preview.Rating);
        Assert.Equal(120.5m, preview.CareerCompletedHours);
        Assert.Equal(new List<string> { "Food Hygiene", "Latte Art" }, preview.Qualifications);
        Assert.True(preview.IsPreferredForSite);
    }

    [Fact]
    public async Task GetShiftCandidatePreview_NonExistentCandidate_ReturnsNull_And_Controller_Returns404()
    {
        // Arrange: handler test
        using var context = CreateDbContext();
        var handler = new GetShiftCandidatePreviewQueryHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<AttendanceRecord>(context),
            new GenericRepository<PerformanceSubmission>(context),
            new GenericRepository<SitePreferredEmployee>(context)
        );

        // Act
        var result = await handler.Handle(new GetShiftCandidatePreviewQuery(9999), CancellationToken.None);

        // Assert
        Assert.Null(result);

        // Controller test for 404
        var fakeMediator = new FakeMediator((req, ct) => Task.FromResult<object?>(null));
        var controller = new ShiftCandidatesController(fakeMediator);

        var actionResult = await controller.GetCandidatePreview(9999, null, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task ShiftCandidatesController_GetShiftCandidates_ReturnsOkWithData()
    {
        // Arrange
        var expectedDto = new PaginatedShiftCandidatesResponseDto(
            new List<ShiftCandidateCardDto>
            {
                new ShiftCandidateCardDto(1, "EMP-0001", "Sara Ali", null, "Nurse", 1, 35m, 4.9m, true)
            },
            1, 1, 10
        );

        var fakeMediator = new FakeMediator((req, ct) => Task.FromResult<object?>(expectedDto));
        var controller = new ShiftCandidatesController(fakeMediator);

        // Act
        var actionResult = await controller.GetShiftCandidates(new GetShiftCandidatesQuery(), CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expectedDto, okResult.Value);
    }

    [Fact]
    public async Task ShiftCandidatesController_GetCandidatePreview_ReturnsOkWhenFound()
    {
        // Arrange
        var expectedDto = new ShiftCandidatePreviewDto(
            Id: 10,
            EmployeeCode: "EMP-0010",
            FullName: "Kareem Tarek",
            AvatarUrl: null,
            JobTitle: "Security Guard",
            JoinDate: DateTime.UtcNow.AddYears(-1),
            HourlyRate: 20m,
            Rating: 5.0m,
            CareerCompletedHours: 200m,
            Qualifications: new List<string> { "Security License" },
            IsPreferredForSite: false
        );

        var fakeMediator = new FakeMediator((req, ct) =>
        {
            if (req is GetShiftCandidatePreviewQuery q && q.CandidateId == 10)
            {
                return Task.FromResult<object?>(expectedDto);
            }
            return Task.FromResult<object?>(null);
        });

        var controller = new ShiftCandidatesController(fakeMediator);

        // Act
        var actionResult = await controller.GetCandidatePreview(10, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expectedDto, okResult.Value);
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
