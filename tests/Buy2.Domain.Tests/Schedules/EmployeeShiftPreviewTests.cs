using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.GetEmployeeShiftPreview;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Schedules;

public class EmployeeShiftPreviewTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static GetEmployeeShiftPreviewQueryHandler CreateHandler(Buy2DbContext context)
    {
        return new GetEmployeeShiftPreviewQueryHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<AttendanceRecord>(context),
            new GenericRepository<PerformanceSubmission>(context),
            new GenericRepository<SitePreferredEmployee>(context),
            new GenericRepository<Qualification>(context)
        );
    }

    [Fact]
    public async Task GetEmployeeShiftPreview_SuccessfulRetrieval_WithAllFieldsPopulated()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRole = new JobRole
        {
            Id = 1,
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
            ProfilePhotoUrl = "https://example.com/avatar.jpg",
            JobRoleId = 1,
            JoinDate = joinDate,
            IsActive = true,
            IsDeleted = false
        };
        context.Employees.Add(employee);

        var payroll = new PayrollProfile
        {
            EmployeeId = 42,
            SalaryType = "Hourly",
            PaymentAmount = 25.50m
        };
        context.PayrollProfiles.Add(payroll);

        var site = new Site { Id = 3, SiteName = "Downtown Cafe" };
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

        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetEmployeeShiftPreviewQuery(42, SiteId: 3), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("EMP-0042", result.EmployeeCode);
        Assert.Equal("Youssef Adel", result.FullName);
        Assert.Equal("https://example.com/avatar.jpg", result.AvatarUrl);
        Assert.Equal("Senior Barista", result.JobTitle);
        Assert.Equal(joinDate, result.JoinDate);
        Assert.Equal(25.50m, result.HourlyRate);
        Assert.Equal(4.5m, result.Rating);
        Assert.Equal(120.5m, result.CareerCompletedHours);
        Assert.Equal(new List<string> { "Food Hygiene", "Latte Art" }, result.Qualifications);
        Assert.True(result.IsPreferredForSite);
    }

    [Fact]
    public async Task GetEmployeeShiftPreview_MonthlySalary_ConvertedToHourlyRateDividedBy160()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRole = new JobRole { Id = 1, Title = "Accountant" };
        context.JobRoles.Add(jobRole);

        var employee = new Employee
        {
            Id = 50,
            FirstName = "Mona",
            LastName = "Zaki",
            JobRoleId = 1,
            IsActive = true,
            IsDeleted = false
        };
        context.Employees.Add(employee);

        var payroll = new PayrollProfile
        {
            EmployeeId = 50,
            SalaryType = "Monthly",
            PaymentAmount = 8000m
        };
        context.PayrollProfiles.Add(payroll);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetEmployeeShiftPreviewQuery(50), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // 8000 / 160 = 50.00
        Assert.Equal(50.00m, result.HourlyRate);
    }

    [Fact]
    public async Task GetEmployeeShiftPreview_HourlySalary_UsedDirectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRole = new JobRole { Id = 1, Title = "Technician" };
        context.JobRoles.Add(jobRole);

        var employee = new Employee
        {
            Id = 51,
            FirstName = "Tamer",
            LastName = "Hosny",
            JobRoleId = 1,
            IsActive = true,
            IsDeleted = false
        };
        context.Employees.Add(employee);

        var payroll = new PayrollProfile
        {
            EmployeeId = 51,
            SalaryType = "Hourly",
            PaymentAmount = 35.75m
        };
        context.PayrollProfiles.Add(payroll);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetEmployeeShiftPreviewQuery(51), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(35.75m, result.HourlyRate);
    }

    [Fact]
    public async Task GetEmployeeShiftPreview_SitePreference_TrueWhenPreferred_FalseWhenNotPreferred()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRole = new JobRole { Id = 1, Title = "Operator" };
        context.JobRoles.Add(jobRole);

        var employee = new Employee
        {
            Id = 60,
            FirstName = "Amr",
            LastName = "Diab",
            JobRoleId = 1,
            IsActive = true,
            IsDeleted = false
        };
        context.Employees.Add(employee);

        context.SitePreferredEmployees.Add(new SitePreferredEmployee { SiteId = 10, EmployeeId = 60 });
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act & Assert - Preferred for Site 10
        var preferredResult = await handler.Handle(new GetEmployeeShiftPreviewQuery(60, SiteId: 10), CancellationToken.None);
        Assert.NotNull(preferredResult);
        Assert.True(preferredResult.IsPreferredForSite);

        // Act & Assert - Not preferred for Site 20
        var notPreferredResult = await handler.Handle(new GetEmployeeShiftPreviewQuery(60, SiteId: 20), CancellationToken.None);
        Assert.NotNull(notPreferredResult);
        Assert.False(notPreferredResult.IsPreferredForSite);

        // Act & Assert - No site specified
        var noSiteResult = await handler.Handle(new GetEmployeeShiftPreviewQuery(60, SiteId: null), CancellationToken.None);
        Assert.NotNull(noSiteResult);
        Assert.False(noSiteResult.IsPreferredForSite);
    }

    [Fact]
    public async Task GetEmployeeShiftPreview_NonExistentEmployee_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetEmployeeShiftPreviewQuery(9999), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetEmployeeShiftPreview_SoftDeletedEmployee_ReturnsNull()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRole = new JobRole { Id = 1, Title = "Operator" };
        context.JobRoles.Add(jobRole);

        var employee = new Employee
        {
            Id = 70,
            FirstName = "Deleted",
            LastName = "User",
            JobRoleId = 1,
            IsActive = true,
            IsDeleted = true
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetEmployeeShiftPreviewQuery(70), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetEmployeeShiftPreview_Qualifications_ResolvesIdsFromRepository()
    {
        // Arrange
        using var context = CreateDbContext();
        context.Qualifications.AddRange(
            new Qualification { Id = 1, Name = "Forklift Operator" },
            new Qualification { Id = 2, Name = "OSHA 30" }
        );

        var jobRole = new JobRole
        {
            Id = 5,
            Title = "Warehouse Lead",
            RequiredQualificationsJson = "[\"1\", \"2\"]"
        };
        context.JobRoles.Add(jobRole);

        var employee = new Employee
        {
            Id = 80,
            FirstName = "Sameh",
            LastName = "Saeed",
            JobRoleId = 5,
            IsActive = true,
            IsDeleted = false
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetEmployeeShiftPreviewQuery(80), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Qualifications.Count);
        Assert.Contains("Forklift Operator", result.Qualifications);
        Assert.Contains("OSHA 30", result.Qualifications);
    }

    [Fact]
    public async Task GetEmployeeShiftPreview_DefaultRatingScore_WhenNoSubmissions()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRole = new JobRole { Id = 1, Title = "Clerk" };
        context.JobRoles.Add(jobRole);

        var employee = new Employee
        {
            Id = 90,
            FirstName = "Nour",
            LastName = "Sherif",
            JobRoleId = 1,
            IsActive = true,
            IsDeleted = false
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetEmployeeShiftPreviewQuery(90), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5.0m, result.Rating);
    }

    [Fact]
    public async Task ShiftCandidatesController_GetEmployeeShiftPreview_ReturnsOkWhenFound()
    {
        // Arrange
        var expectedDto = new ShiftCandidatePreviewDto(
            Id: 100,
            EmployeeCode: "EMP-0100",
            FullName: "Kareem Tarek",
            AvatarUrl: "https://example.com/avatar.jpg",
            JobTitle: "Security Supervisor",
            JoinDate: DateTime.UtcNow.AddYears(-1),
            HourlyRate: 25.00m,
            Rating: 4.8m,
            CareerCompletedHours: 500m,
            Qualifications: new List<string> { "Security License" },
            IsPreferredForSite: true
        );

        var fakeMediator = new FakeMediator((req, ct) =>
        {
            if (req is GetEmployeeShiftPreviewQuery q && q.EmployeeId == 100)
            {
                return Task.FromResult<object?>(expectedDto);
            }
            return Task.FromResult<object?>(null);
        });

        var controller = new ShiftCandidatesController(fakeMediator);

        // Act
        var actionResult = await controller.GetEmployeeShiftPreview(100, 5, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expectedDto, okResult.Value);
    }

    [Fact]
    public async Task ShiftCandidatesController_GetEmployeeShiftPreview_ReturnsNotFoundWhenNull()
    {
        // Arrange
        var fakeMediator = new FakeMediator((req, ct) => Task.FromResult<object?>(null));
        var controller = new ShiftCandidatesController(fakeMediator);

        // Act
        var actionResult = await controller.GetEmployeeShiftPreview(999, null, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
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
