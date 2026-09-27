using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.Requests.GetSubmittedRequests;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Buy2.Domain.Tests.Requests;

public class SubmittedRequestsQueryTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetSubmittedRequests_ReturnsPaginatedListWithSearchAndFilters()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var emp1 = new Employee { FirstName = "Sarah", LastName = "Connor", Email = "sarah@example.com" };
            var emp2 = new Employee { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
            var manager = new Employee { FirstName = "Kyle", LastName = "Reese", Email = "kyle@example.com" };

            var typeA = new RequestType { Name = "Annual Leave", Category = "Leave" };
            var typeB = new RequestType { Name = "Remote Work", Category = "General" };

            context.Employees.AddRange(emp1, emp2, manager);
            context.RequestTypes.AddRange(typeA, typeB);
            await context.SaveChangesAsync();

            context.Requests.AddRange(
                new Request
                {
                    EmployeeId = emp1.Id,
                    RequestTypeId = typeA.Id,
                    ManagerId = manager.Id,
                    ManagerStatus = "Pending",
                    HrStatus = "Pending",
                    Status = "Pending",
                    SubmittedAt = DateTime.UtcNow.AddDays(-5),
                    Reason = "Summer vacation"
                },
                new Request
                {
                    EmployeeId = emp2.Id,
                    RequestTypeId = typeB.Id,
                    ManagerId = manager.Id,
                    ManagerStatus = "Approved",
                    HrStatus = "Approved",
                    Status = "Approved",
                    SubmittedAt = DateTime.UtcNow.AddDays(-1),
                    Reason = "Work from hometown"
                }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetSubmittedRequestsQueryHandler(new GenericRepository<Request>(context));

            // Test 1: Fetch all (default sort submittedAt descending)
            var all = await handler.Handle(new GetSubmittedRequestsQuery(), CancellationToken.None);
            Assert.Equal(2, all.TotalCount);
            Assert.Equal("John Doe", all.Items[0].EmployeeName);
            Assert.Equal("Sarah Connor", all.Items[1].EmployeeName);

            // Test 2: Search by employee name
            var searchResult = await handler.Handle(new GetSubmittedRequestsQuery(Search: "Sarah"), CancellationToken.None);
            Assert.Single(searchResult.Items);
            Assert.Equal("Sarah Connor", searchResult.Items[0].EmployeeName);

            // Test 3: Filter by status
            var approvedResult = await handler.Handle(new GetSubmittedRequestsQuery(OverallStatus: "Approved"), CancellationToken.None);
            Assert.Single(approvedResult.Items);
            Assert.Equal("Approved", approvedResult.Items[0].Status);

            // Test 4: Pagination
            var pagedResult = await handler.Handle(new GetSubmittedRequestsQuery(PageNumber: 1, PageSize: 1), CancellationToken.None);
            Assert.Single(pagedResult.Items);
            Assert.Equal(2, pagedResult.TotalCount);
            Assert.Equal(2, pagedResult.TotalPages);
            Assert.True(pagedResult.HasNextPage);
            Assert.False(pagedResult.HasPreviousPage);
        }
    }

    [Fact]
    public async Task GetSubmittedRequests_SortsByEmployeeNameAndRequestType()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var empA = new Employee { FirstName = "Alice", LastName = "Wonderland", Email = "alice@example.com" };
            var empZ = new Employee { FirstName = "Zack", LastName = "Morris", Email = "zack@example.com" };
            var reqType = new RequestType { Name = "Equipment", Category = "Asset" };

            context.Employees.AddRange(empA, empZ);
            context.RequestTypes.Add(reqType);
            await context.SaveChangesAsync();

            context.Requests.AddRange(
                new Request { EmployeeId = empZ.Id, RequestTypeId = reqType.Id, SubmittedAt = DateTime.UtcNow.AddDays(-2) },
                new Request { EmployeeId = empA.Id, RequestTypeId = reqType.Id, SubmittedAt = DateTime.UtcNow.AddDays(-1) }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetSubmittedRequestsQueryHandler(new GenericRepository<Request>(context));

            var sortedByNameAsc = await handler.Handle(new GetSubmittedRequestsQuery(SortBy: "EmployeeName", SortDescending: false), CancellationToken.None);
            Assert.Equal("Alice Wonderland", sortedByNameAsc.Items[0].EmployeeName);
            Assert.Equal("Zack Morris", sortedByNameAsc.Items[1].EmployeeName);
        }
    }

    [Fact]
    public async Task Controller_GetSubmittedRequests_ReturnsOkWithPaginatedResult()
    {
        var mockResult = new PaginatedListResult<SubmittedRequestSummaryDto>(
            new List<SubmittedRequestSummaryDto>
            {
                new SubmittedRequestSummaryDto(1, 10, "John Doe", "EMP-01", "Leave", "Leave", DateTime.UtcNow, null, null, "Manager", "Pending", "Pending", "Pending", "Vacation", 0)
            },
            1, 1, 20, 1
        );

        var fakeMediator = new FakeMediator(mockResult);
        var controller = new RequestsController(fakeMediator);

        var actionResult = await controller.GetSubmittedRequests(null, null, null, null, null, null, null);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<PaginatedListResult<SubmittedRequestSummaryDto>>(okResult.Value);

        Assert.Single(result.Items);
        Assert.Equal("John Doe", result.Items[0].EmployeeName);
    }

    private class FakeMediator : ISender
    {
        private readonly object _result;

        public FakeMediator(object result)
        {
            _result = result;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((TResponse)_result);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<object?>(_result);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
