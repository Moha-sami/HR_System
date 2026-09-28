using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.Requests.GetRequestsHistory;
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

public class RequestsHistoryQueryTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetRequestsHistory_ReturnsOnlyResolvedRequests_ExcludesPending()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "Ellen", LastName = "Ripley", Email = "ripley@weyland.com", NationalId = "EMP-001" };
            var manager = new Employee { FirstName = "Arthur", LastName = "Dallas", Email = "dallas@weyland.com" };
            var leaveType = new RequestType { Name = "Sick Leave", Category = "Leave" };
            var expenseType = new RequestType { Name = "Equipment", Category = "Expense" };

            context.Employees.AddRange(emp, manager);
            context.RequestTypes.AddRange(leaveType, expenseType);
            await context.SaveChangesAsync();

            context.Requests.AddRange(
                new Request
                {
                    EmployeeId = emp.Id,
                    RequestTypeId = leaveType.Id,
                    ManagerId = manager.Id,
                    Status = "Approved",
                    ManagerStatus = "Approved",
                    HrStatus = "Approved",
                    SubmittedAt = DateTime.UtcNow.AddDays(-10),
                    ResolvedAt = DateTime.UtcNow.AddDays(-8),
                    Reason = "Medical recovery"
                },
                new Request
                {
                    EmployeeId = emp.Id,
                    RequestTypeId = expenseType.Id,
                    ManagerId = manager.Id,
                    Status = "Rejected",
                    ManagerStatus = "Rejected",
                    HrStatus = "Pending",
                    RejectionReason = "Policy Violation",
                    SubmittedAt = DateTime.UtcNow.AddDays(-5),
                    ResolvedAt = DateTime.UtcNow.AddDays(-4),
                    Reason = "Monitor purchase"
                },
                new Request
                {
                    EmployeeId = emp.Id,
                    RequestTypeId = leaveType.Id,
                    ManagerId = manager.Id,
                    Status = "Pending",
                    ManagerStatus = "Pending",
                    HrStatus = "Pending",
                    SubmittedAt = DateTime.UtcNow.AddDays(-1),
                    Reason = "Upcoming PTO"
                }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var repo = new GenericRepository<Request>(context);
            var handler = new GetRequestsHistoryQueryHandler(repo);

            var query = new GetRequestsHistoryQuery();
            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Equal(2, result.TotalCount);
            Assert.DoesNotContain(result.Items, r => r.Status == "Pending");
            Assert.Contains(result.Items, r => r.Status == "Approved");
            Assert.Contains(result.Items, r => r.Status == "Rejected");
        }
    }

    [Fact]
    public async Task GetRequestsHistory_FiltersBySearchDateAndRequestTypes()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var emp1 = new Employee { FirstName = "Luke", LastName = "Skywalker", Email = "luke@rebel.org", NationalId = "JEDI-01" };
            var emp2 = new Employee { FirstName = "Han", LastName = "Solo", Email = "han@falcon.org", NationalId = "PILOT-02" };
            var manager = new Employee { FirstName = "Leia", LastName = "Organa", Email = "leia@rebel.org" };

            var typeA = new RequestType { Name = "Flight Leave", Category = "Leave" };
            var typeB = new RequestType { Name = "Fuel Expense", Category = "Expense" };

            context.Employees.AddRange(emp1, emp2, manager);
            context.RequestTypes.AddRange(typeA, typeB);
            await context.SaveChangesAsync();

            context.Requests.AddRange(
                new Request
                {
                    EmployeeId = emp1.Id,
                    RequestTypeId = typeA.Id,
                    ManagerId = manager.Id,
                    Status = "Approved",
                    SubmittedAt = new DateTime(2026, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                    ResolvedAt = new DateTime(2026, 1, 11, 10, 0, 0, DateTimeKind.Utc)
                },
                new Request
                {
                    EmployeeId = emp2.Id,
                    RequestTypeId = typeB.Id,
                    ManagerId = manager.Id,
                    Status = "Approved",
                    SubmittedAt = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc),
                    ResolvedAt = new DateTime(2026, 2, 16, 10, 0, 0, DateTimeKind.Utc)
                }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var repo = new GenericRepository<Request>(context);
            var handler = new GetRequestsHistoryQueryHandler(repo);

            // Test search by employee name
            var searchResult = await handler.Handle(new GetRequestsHistoryQuery(Search: "Skywalker"), CancellationToken.None);
            Assert.Single(searchResult.Items);
            Assert.Equal("Luke Skywalker", searchResult.Items[0].EmployeeName);

            // Test search by request type name
            var typeSearchResult = await handler.Handle(new GetRequestsHistoryQuery(Search: "Fuel"), CancellationToken.None);
            Assert.Single(typeSearchResult.Items);
            Assert.Equal("Han Solo", typeSearchResult.Items[0].EmployeeName);

            // Test date range filter
            var dateResult = await handler.Handle(new GetRequestsHistoryQuery(
                FromDate: new DateTime(2026, 2, 1),
                ToDate: new DateTime(2026, 2, 28)
            ), CancellationToken.None);
            Assert.Single(dateResult.Items);
            Assert.Equal("Han Solo", dateResult.Items[0].EmployeeName);
        }
    }

    [Fact]
    public async Task GetRequestsHistory_SupportsDynamicSorting()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var empA = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@test.com" };
            var empB = new Employee { FirstName = "Bob", LastName = "Jones", Email = "bob@test.com" };
            var type = new RequestType { Name = "General Request", Category = "General" };

            context.Employees.AddRange(empA, empB);
            context.RequestTypes.Add(type);
            await context.SaveChangesAsync();

            context.Requests.AddRange(
                new Request
                {
                    EmployeeId = empA.Id,
                    RequestTypeId = type.Id,
                    Status = "Approved",
                    SubmittedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Request
                {
                    EmployeeId = empB.Id,
                    RequestTypeId = type.Id,
                    Status = "Rejected",
                    SubmittedAt = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc)
                }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var repo = new GenericRepository<Request>(context);
            var handler = new GetRequestsHistoryQueryHandler(repo);

            // Sort by employee name ascending
            var ascResult = await handler.Handle(new GetRequestsHistoryQuery(SortBy: "EmployeeName", SortDescending: false), CancellationToken.None);
            Assert.Equal("Alice Smith", ascResult.Items[0].EmployeeName);
            Assert.Equal("Bob Jones", ascResult.Items[1].EmployeeName);

            // Sort by employee name descending
            var descResult = await handler.Handle(new GetRequestsHistoryQuery(SortBy: "EmployeeName", SortDescending: true), CancellationToken.None);
            Assert.Equal("Bob Jones", descResult.Items[0].EmployeeName);
            Assert.Equal("Alice Smith", descResult.Items[1].EmployeeName);
        }
    }

    [Fact]
    public async Task RequestsController_GetRequestsHistory_DispatchesQueryCorrectly()
    {
        var fakeResult = new PaginatedListResult<RequestHistorySummaryDto>(
            new List<RequestHistorySummaryDto>
            {
                new RequestHistorySummaryDto(1, 10, "Test Employee", "CODE1", "Leave", "Category", DateTime.UtcNow, null, null, "Manager", "Approved", "ok", "Approved", "ok", "Approved", null, DateTime.UtcNow, 0)
            },
            1, 1, 20, 1
        );

        var controller = new RequestsController(new FakeMediator(fakeResult));

        var result = await controller.GetRequestsHistory(
            search: "Test",
            fromDate: null,
            toDate: null,
            requestTypeIds: null,
            overallStatus: "Approved"
        );

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedListResult<RequestHistorySummaryDto>>(okResult.Value);
        Assert.Single(response.Items);
        Assert.Equal("Test Employee", response.Items[0].EmployeeName);
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
