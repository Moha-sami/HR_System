using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Api.Controllers;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Application.Features.Jobs.GetJobById;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Jobs;

public class GetJobByIdQueryTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetJobByIdQuery_ReturnsJobDetails_WithParsedJsonAndEmployeeCount_WhenFound()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);

        var dept = new Department { Name = "Engineering" };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        var createdAt = DateTimeOffset.UtcNow.AddDays(-10);
        var updatedAt = DateTimeOffset.UtcNow.AddDays(-1);

        var job = new JobRole
        {
            Title = "Senior Software Engineer",
            Description = "Develops software solutions",
            DepartmentId = dept.Id,
            SeniorityLevel = "Senior",
            ExperienceYears = 5,
            AttendanceType = "Hybrid",
            OnlineWorkdaysJson = "[\"Monday\", \"Wednesday\"]",
            OfflineWorkdaysJson = "[\"Tuesday\", \"Thursday\", \"Friday\"]",
            RequiredQualificationsJson = "[\"Bachelor Degree\", \"C#\"]",
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        // 2 active employees and 1 deleted employee to strictly verify !IsDeleted filtering mutation
        var activeEmp1 = new Employee { FirstName = "John", LastName = "Doe", JobRoleId = job.Id, IsDeleted = false };
        var activeEmp2 = new Employee { FirstName = "Alice", LastName = "Smith", JobRoleId = job.Id, IsDeleted = false };
        var deletedEmp = new Employee { FirstName = "Jane", LastName = "Smith", JobRoleId = job.Id, IsDeleted = true };
        context.Employees.AddRange(activeEmp1, activeEmp2, deletedEmp);
        await context.SaveChangesAsync();

        var handler = new GetJobByIdQueryHandler(jobRepo);
        var query = new GetJobByIdQuery(job.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(job.Id, result.Id);
        Assert.Equal("Senior Software Engineer", result.Title);
        Assert.Equal(dept.Id, result.DepartmentId);
        Assert.Equal("Engineering", result.DepartmentName);
        Assert.Equal("Senior", result.SeniorityLevel);
        Assert.Equal("Develops software solutions", result.Description);
        Assert.Equal(new List<string> { "Bachelor Degree", "C#" }, result.RequiredQualifications);
        Assert.Equal(5, result.ExperienceYearsMin);
        Assert.Equal("Hybrid", result.WorkModel);
        Assert.Equal(new List<string> { "Monday", "Wednesday" }, result.OnlineWorkdays);
        Assert.Equal(new List<string> { "Tuesday", "Thursday", "Friday" }, result.OfflineWorkdays);
        Assert.Equal(2, result.AssignedEmployeesCount);
        Assert.True(result.IsActive);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.Equal(updatedAt, result.UpdatedAt);
    }

    [Fact]
    public async Task GetJobByIdQuery_ReturnsNull_WhenJobNotFound()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var handler = new GetJobByIdQueryHandler(jobRepo);

        // Act
        var result = await handler.Handle(new GetJobByIdQuery(999), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetJobByIdQuery_QueriesCorrectJobId_WhenMultipleJobsExist()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);

        var job1 = new JobRole { Title = "Backend Dev", IsActive = true };
        var job2 = new JobRole { Title = "Frontend Dev", IsActive = true };
        context.JobRoles.AddRange(job1, job2);
        await context.SaveChangesAsync();

        var handler = new GetJobByIdQueryHandler(jobRepo);

        // Act
        var result = await handler.Handle(new GetJobByIdQuery(job2.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(job2.Id, result.Id);
        Assert.Equal("Frontend Dev", result.Title);
    }

    [Fact]
    public async Task GetJobByIdQuery_HandlesNullOrMalformedJson_Safely()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);

        var job = new JobRole
        {
            Title = "QA Lead",
            SeniorityLevel = "Lead",
            ExperienceYears = 3,
            AttendanceType = "OnSite",
            OnlineWorkdaysJson = "",
            OfflineWorkdaysJson = "invalid-json-string",
            RequiredQualificationsJson = "  ",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        var handler = new GetJobByIdQueryHandler(jobRepo);

        // Act
        var result = await handler.Handle(new GetJobByIdQuery(job.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.OnlineWorkdays);
        Assert.Empty(result.OfflineWorkdays);
        Assert.Empty(result.RequiredQualifications);
        Assert.Equal("N/A", result.DepartmentName);
        Assert.Equal(0, result.AssignedEmployeesCount);
    }

    [Fact]
    public async Task GetJobByIdQuery_HandlesJsonNullLiteral_Safely()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);

        var job = new JobRole
        {
            Title = "DevOps Engineer",
            RequiredQualificationsJson = "null",
            OnlineWorkdaysJson = "null",
            OfflineWorkdaysJson = "null",
            IsActive = true
        };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        var handler = new GetJobByIdQueryHandler(jobRepo);

        // Act
        var result = await handler.Handle(new GetJobByIdQuery(job.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.RequiredQualifications);
        Assert.Empty(result.OnlineWorkdays);
        Assert.Empty(result.OfflineWorkdays);
    }

    [Fact]
    public async Task GetJobByIdController_ReturnsOk_WhenJobExists()
    {
        // Arrange
        var expectedDto = new JobDetailsDto(
            Id: 42,
            Title: "Architect",
            DepartmentId: 1,
            DepartmentName: "Architecture",
            SeniorityLevel: "Principal",
            Description: "System Design",
            RequiredQualifications: new List<string> { "MS CS" },
            ExperienceYearsMin: 10,
            WorkModel: "Remote",
            OnlineWorkdays: new List<string> { "Mon", "Tue", "Wed", "Thu", "Fri" },
            OfflineWorkdays: new List<string>(),
            AssignedEmployeesCount: 5,
            IsActive: true,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: null
        );

        object? receivedRequest = null;
        CancellationToken receivedToken = default;

        var fakeMediator = new FakeMediator((req, ct) =>
        {
            receivedRequest = req;
            receivedToken = ct;
            if (req is GetJobByIdQuery q && q.Id == 42)
            {
                return Task.FromResult<object?>(expectedDto);
            }
            return Task.FromResult<object?>(null);
        });

        var controller = new JobsController(fakeMediator);
        using var cts = new CancellationTokenSource();

        // Act
        var actionResult = await controller.GetJobById(42, cts.Token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expectedDto, okResult.Value);

        var query = Assert.IsType<GetJobByIdQuery>(receivedRequest);
        Assert.Equal(42, query.Id);
        Assert.Equal(cts.Token, receivedToken);
    }

    [Fact]
    public async Task GetJobByIdController_ReturnsNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        object? receivedRequest = null;

        var fakeMediator = new FakeMediator((req, ct) =>
        {
            receivedRequest = req;
            return Task.FromResult<object?>(null);
        });

        var controller = new JobsController(fakeMediator);

        // Act
        var actionResult = await controller.GetJobById(999, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(404, notFoundResult.StatusCode);

        var query = Assert.IsType<GetJobByIdQuery>(receivedRequest);
        Assert.Equal(999, query.Id);
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
