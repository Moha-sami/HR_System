using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.Requests.ExportRequestsHistory;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Buy2.Domain.Tests.Requests;

public class ExportRequestsHistoryTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task ExportRequestsHistory_Csv_ReturnsValidCsvFile()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "John", LastName = "Connor", Email = "john@rebel.org", NationalId = "REB-101" };
            var type = new RequestType { Name = "Mission Leave", Category = "Leave" };

            context.Employees.Add(emp);
            context.RequestTypes.Add(type);
            await context.SaveChangesAsync();

            context.Requests.Add(new Request
            {
                EmployeeId = emp.Id,
                RequestTypeId = type.Id,
                Status = "Approved",
                ManagerStatus = "Approved",
                HrStatus = "Approved",
                SubmittedAt = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc),
                ResolvedAt = new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc),
                Reason = "Special assignment"
            });
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var repo = new GenericRepository<Request>(context);
            var handler = new ExportRequestsHistoryQueryHandler(repo);

            var query = new ExportRequestsHistoryQuery(Format: "csv");
            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("text/csv; charset=utf-8", result.ContentType);
            Assert.EndsWith(".csv", result.FileName);

            var csvText = Encoding.UTF8.GetString(result.Content);
            Assert.Contains("Request ID,Employee ID,Employee Name", csvText);
            Assert.Contains("John Connor", csvText);
            Assert.Contains("Mission Leave", csvText);
            Assert.Contains("Approved", csvText);
        }
    }

    [Fact]
    public async Task ExportRequestsHistory_Excel_ReturnsValidExcelWorkbook()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "Sarah", LastName = "Connor", Email = "sarah@rebel.org" };
            var type = new RequestType { Name = "Defense Budget", Category = "Expense" };

            context.Employees.Add(emp);
            context.RequestTypes.Add(type);
            await context.SaveChangesAsync();

            context.Requests.Add(new Request
            {
                EmployeeId = emp.Id,
                RequestTypeId = type.Id,
                Status = "Rejected",
                ManagerStatus = "Rejected",
                HrStatus = "Pending",
                RejectionReason = "Exceeds Limit",
                SubmittedAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
                ResolvedAt = new DateTime(2026, 5, 2, 12, 0, 0, DateTimeKind.Utc),
                Reason = "Ammo purchase"
            });
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var repo = new GenericRepository<Request>(context);
            var handler = new ExportRequestsHistoryQueryHandler(repo);

            var query = new ExportRequestsHistoryQuery(Format: "xlsx");
            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.ContentType);
            Assert.EndsWith(".xlsx", result.FileName);
            Assert.True(result.Content.Length > 0);
        }
    }

    [Fact]
    public async Task ExportRequestsHistory_AppliesFiltersCorrectly()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var emp1 = new Employee { FirstName = "Target", LastName = "Employee", Email = "target@corp.com" };
            var emp2 = new Employee { FirstName = "Other", LastName = "Employee", Email = "other@corp.com" };
            var type = new RequestType { Name = "Standard", Category = "General" };

            context.Employees.AddRange(emp1, emp2);
            context.RequestTypes.Add(type);
            await context.SaveChangesAsync();

            context.Requests.AddRange(
                new Request
                {
                    EmployeeId = emp1.Id,
                    RequestTypeId = type.Id,
                    Status = "Approved",
                    SubmittedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Request
                {
                    EmployeeId = emp2.Id,
                    RequestTypeId = type.Id,
                    Status = "Approved",
                    SubmittedAt = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc)
                }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var repo = new GenericRepository<Request>(context);
            var handler = new ExportRequestsHistoryQueryHandler(repo);

            var query = new ExportRequestsHistoryQuery(
                Format: "csv",
                Search: "Target"
            );
            var result = await handler.Handle(query, CancellationToken.None);
            var csvText = Encoding.UTF8.GetString(result.Content);

            Assert.Contains("Target Employee", csvText);
            Assert.DoesNotContain("Other Employee", csvText);
        }
    }

    [Fact]
    public async Task RequestsController_ExportRequestsHistory_ReturnsFileResult()
    {
        var exportResult = new ExportRequestsHistoryResult(
            Content: Encoding.UTF8.GetBytes("sample csv content"),
            ContentType: "text/csv; charset=utf-8",
            FileName: "test_export.csv"
        );

        var controller = new RequestsController(new FakeExportMediator(exportResult));

        var actionResult = await controller.ExportRequestsHistory(
            format: "csv",
            search: null,
            fromDate: null,
            toDate: null,
            requestTypeIds: null,
            overallStatus: null
        );

        var fileResult = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);
        Assert.Equal("test_export.csv", fileResult.FileDownloadName);
        Assert.Equal("sample csv content", Encoding.UTF8.GetString(fileResult.FileContents));
    }

    private class FakeExportMediator : ISender
    {
        private readonly object _result;

        public FakeExportMediator(object result)
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
