using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Api.Controllers;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Application.Features.Jobs.GetJobEmployees;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Jobs;

public class GetJobEmployeesQueryTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetJobEmployeesQuery_ReturnsPaginatedAssignedEmployees_WhenJobExists()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var dept = new Department { Name = "Software Engineering" };
        var site = new Site { SiteName = "Headquarters" };
        context.Departments.Add(dept);
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var job = new JobRole { Title = "Senior C# Developer", DepartmentId = dept.Id };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        var joinDate1 = DateTime.UtcNow.AddYears(-2);
        var joinDate2 = DateTime.UtcNow.AddYears(-1);

        var emp1 = new Employee
        {
            FirstName = "Alice",
            LastName = "Smith",
            EmployeeCode = "EMP001",
            Email = "alice@example.com",
            JobRoleId = job.Id,
            JobRole = job,
            SiteId = site.Id,
            Site = site,
            JoinDate = joinDate1,
            ProfilePhotoUrl = "https://example.com/photo1.png",
            IsDeleted = false
        };

        var emp2 = new Employee
        {
            FirstName = "Bob",
            LastName = "Jones",
            EmployeeCode = "EMP002",
            Email = "bob@example.com",
            JobRoleId = job.Id,
            JobRole = job,
            SiteId = site.Id,
            Site = site,
            JoinDate = joinDate2,
            ProfilePhotoUrl = "https://example.com/photo2.png",
            IsDeleted = false
        };

        context.Employees.AddRange(emp1, emp2);
        await context.SaveChangesAsync();

        var handler = new GetJobEmployeesQueryHandler(jobRepo, empRepo);
        var query = new GetJobEmployeesQuery(job.Id, PageNumber: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(2, result.Items.Count);

        // Ordered by FirstName -> Alice, Bob
        Assert.Equal("Alice Smith", result.Items[0].FullName);
        Assert.Equal("EMP001", result.Items[0].EmployeeCode);
        Assert.Equal("alice@example.com", result.Items[0].Email);
        Assert.Equal("Software Engineering", result.Items[0].DepartmentName);
        Assert.Equal("Headquarters", result.Items[0].SiteName);
        Assert.Equal(joinDate1, result.Items[0].JoinDate);
        Assert.Equal("https://example.com/photo1.png", result.Items[0].ProfilePhotoUrl);

        Assert.Equal("Bob Jones", result.Items[1].FullName);
    }

    [Fact]
    public async Task GetJobEmployeesQuery_ExcludesSoftDeletedEmployees()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var site = new Site { SiteName = "Test Site" };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var job = new JobRole { Title = "QA Engineer" };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        var activeEmp = new Employee
        {
            FirstName = "Charlie",
            LastName = "Brown",
            Email = "charlie@example.com",
            EmployeeCode = "EMP101",
            JobRoleId = job.Id,
            JobRole = job,
            SiteId = site.Id,
            Site = site,
            IsDeleted = false
        };
        var deletedEmp = new Employee
        {
            FirstName = "Dave",
            LastName = "Miller",
            Email = "dave@example.com",
            EmployeeCode = "EMP102",
            JobRoleId = job.Id,
            JobRole = job,
            SiteId = site.Id,
            Site = site,
            IsDeleted = true
        };
        context.Employees.AddRange(activeEmp, deletedEmp);
        await context.SaveChangesAsync();

        var handler = new GetJobEmployeesQueryHandler(jobRepo, empRepo);
        var query = new GetJobEmployeesQuery(job.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Charlie Brown", result.Items[0].FullName);
    }

    [Fact]
    public async Task GetJobEmployeesQuery_ReturnsNull_WhenJobDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var handler = new GetJobEmployeesQueryHandler(jobRepo, empRepo);
        var query = new GetJobEmployeesQuery(999);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("alice", 1)]
    [InlineData("SMITH", 1)]
    [InlineData("EMP002", 1)]
    [InlineData("charlie@example.com", 1)]
    [InlineData("nonexistent", 0)]
    public async Task GetJobEmployeesQuery_FiltersBySearchTerm(string searchTerm, int expectedCount)
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var site = new Site { SiteName = "Test Site" };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var job = new JobRole { Title = "DevOps" };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        var emp1 = new Employee { FirstName = "Alice", LastName = "Smith", EmployeeCode = "EMP001", Email = "alice@example.com", JobRoleId = job.Id, JobRole = job, SiteId = site.Id, Site = site, IsDeleted = false };
        var emp2 = new Employee { FirstName = "Bob", LastName = "Taylor", EmployeeCode = "EMP002", Email = "bob@example.com", JobRoleId = job.Id, JobRole = job, SiteId = site.Id, Site = site, IsDeleted = false };
        var emp3 = new Employee { FirstName = "Charlie", LastName = "Johnson", EmployeeCode = "EMP003", Email = "charlie@example.com", JobRoleId = job.Id, JobRole = job, SiteId = site.Id, Site = site, IsDeleted = false };
        context.Employees.AddRange(emp1, emp2, emp3);
        await context.SaveChangesAsync();

        var handler = new GetJobEmployeesQueryHandler(jobRepo, empRepo);
        var query = new GetJobEmployeesQuery(job.Id, SearchTerm: searchTerm);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.TotalCount);
    }

    [Fact]
    public async Task GetJobEmployeesQuery_HandlesDefaultFallbackValues_WhenEmployeeFieldsAreEmptyOrNullNavigations()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var site = new Site { SiteName = "HQ" };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var job = new JobRole { Title = "Intern", DepartmentId = null };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        var emp = new Employee
        {
            FirstName = "   ",
            LastName = "",
            EmployeeCode = "EMP999",
            Email = "emp@example.com",
            JobRoleId = job.Id,
            JobRole = job,
            SiteId = site.Id,
            Site = site,
            IsDeleted = false
        };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var handler = new GetJobEmployeesQueryHandler(jobRepo, empRepo);
        var query = new GetJobEmployeesQuery(job.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        var item = result.Items[0];
        Assert.Equal("N/A", item.FullName);
        Assert.Equal("N/A", item.DepartmentName);
        Assert.Equal("HQ", item.SiteName);
    }

    [Fact]
    public async Task GetJobEmployeesQuery_ClampsPageNumberAndPageSize()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var site = new Site { SiteName = "Test Site" };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var job = new JobRole { Title = "Manager" };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        var emp = new Employee { FirstName = "Eve", LastName = "Adams", Email = "eve@example.com", EmployeeCode = "EMP500", JobRoleId = job.Id, JobRole = job, SiteId = site.Id, Site = site, IsDeleted = false };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var handler = new GetJobEmployeesQueryHandler(jobRepo, empRepo);
        var query = new GetJobEmployeesQuery(job.Id, PageNumber: -5, PageSize: 500);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    public async Task GetJobEmployeesController_ReturnsOk_WhenJobExists()
    {
        // Arrange
        var expectedDto = new JobPaginatedResponseDto<JobAssignedEmployeeListItemDto>(
            Items: new List<JobAssignedEmployeeListItemDto>
            {
                new(10, "EMP010", "Frank White", "frank@example.com", "Sales", "Main Branch", DateTime.UtcNow, null)
            },
            TotalCount: 1,
            PageNumber: 1,
            PageSize: 10,
            TotalPages: 1
        );

        object? receivedRequest = null;
        CancellationToken receivedToken = default;

        var fakeMediator = new FakeMediator((req, ct) =>
        {
            receivedRequest = req;
            receivedToken = ct;
            if (req is GetJobEmployeesQuery q && q.JobId == 5)
            {
                return Task.FromResult<object?>(expectedDto);
            }
            return Task.FromResult<object?>(null);
        });

        var controller = new JobsController(fakeMediator);
        using var cts = new CancellationTokenSource();

        // Act
        var actionResult = await controller.GetJobEmployees(5, "frank", 1, 10, cts.Token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expectedDto, okResult.Value);

        var query = Assert.IsType<GetJobEmployeesQuery>(receivedRequest);
        Assert.Equal(5, query.JobId);
        Assert.Equal("frank", query.SearchTerm);
        Assert.Equal(1, query.PageNumber);
        Assert.Equal(10, query.PageSize);
        Assert.Equal(cts.Token, receivedToken);
    }

    [Fact]
    public async Task GetJobEmployeesController_ReturnsNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        var fakeMediator = new FakeMediator((req, ct) => Task.FromResult<object?>(null));
        var controller = new JobsController(fakeMediator);

        // Act
        var actionResult = await controller.GetJobEmployees(99, null, 1, 10, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
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
