using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.Requests.GetRequestDetail;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Buy2.Domain.Tests.Requests;

public class GetRequestDetailTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetRequestDetail_ReturnsFullAggregate_WithAttachments_DualReview_AndHistory()
    {
        var dbName = Guid.NewGuid().ToString();
        int targetRequestId;

        using (var context = CreateDbContext(dbName))
        {
            var jobRole = new JobRole { Title = "Software Engineer" };
            context.Set<JobRole>().Add(jobRole);
            await context.SaveChangesAsync();

            var emp = new Employee
            {
                FirstName = "David",
                LastName = "Miller",
                Email = "david@example.com",
                NationalId = "EMP-999",
                JobRoleId = jobRole.Id
            };
            var manager = new Employee { FirstName = "Sarah", LastName = "Connor", Email = "sarah@example.com" };
            var hr = new Employee { FirstName = "Rachel", LastName = "Green", Email = "rachel@example.com" };

            var typeA = new RequestType { Name = "Maternity Leave", Category = "Leave" };
            var typeB = new RequestType { Name = "Hardware Replacement", Category = "Equipment" };

            context.Employees.AddRange(emp, manager, hr);
            context.RequestTypes.AddRange(typeA, typeB);
            await context.SaveChangesAsync();

            // Older previous request
            var prevReq = new Request
            {
                EmployeeId = emp.Id,
                RequestTypeId = typeB.Id,
                Status = "Approved",
                SubmittedAt = DateTime.UtcNow.AddMonths(-2),
                StartDate = DateTime.UtcNow.AddMonths(-2),
                EndDate = DateTime.UtcNow.AddMonths(-2).AddDays(1),
                ResolvedAt = DateTime.UtcNow.AddMonths(-2).AddDays(2)
            };

            // Target request
            var targetReq = new Request
            {
                EmployeeId = emp.Id,
                RequestTypeId = typeA.Id,
                ManagerId = manager.Id,
                ManagerStatus = "Approved",
                ManagerComment = "Looks good, team is covered.",
                ManagerDecisionAt = DateTime.UtcNow.AddDays(-1),
                HrId = hr.Id,
                HrStatus = "Pending",
                HrComment = "Under review",
                Status = "Pending",
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(40),
                Reason = "Maternity preparation",
                CategoryValuesJson = "{\"expectedDate\":\"2026-11-01\"}",
                SubmittedAt = DateTime.UtcNow.AddDays(-2)
            };

            targetReq.Attachments.Add(new RequestAttachment
            {
                FileName = "doctor_note.pdf",
                StorageUrl = "/storage/requests/doctor_note.pdf",
                ContentType = "application/pdf",
                FileSize = 20480,
                UploadedAt = DateTime.UtcNow.AddDays(-2)
            });

            context.Requests.AddRange(prevReq, targetReq);
            await context.SaveChangesAsync();
            targetRequestId = targetReq.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetRequestDetailQueryHandler(new GenericRepository<Request>(context));

            var result = await handler.Handle(new GetRequestDetailQuery(targetRequestId), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(targetRequestId, result.Id);
            Assert.Equal("David Miller", result.EmployeeName);
            Assert.Equal("EMP-999", result.EmployeeCode);
            Assert.Equal("Software Engineer", result.DepartmentName);
            Assert.Equal("Maternity Leave", result.RequestType);
            Assert.Equal("Leave", result.Category);
            Assert.Equal("Pending", result.OverallStatus);
            Assert.Equal("Maternity preparation", result.Reason);
            Assert.Contains("expectedDate", result.CategoryValuesJson);

            // Review Notes
            Assert.Equal("Manager", result.ManagerReview.ReviewerRole);
            Assert.Equal("Sarah Connor", result.ManagerReview.ReviewerName);
            Assert.Equal("Approved", result.ManagerReview.Status);
            Assert.Equal("Looks good, team is covered.", result.ManagerReview.Comment);

            Assert.Equal("HR", result.HrReview.ReviewerRole);
            Assert.Equal("Rachel Green", result.HrReview.ReviewerName);
            Assert.Equal("Pending", result.HrReview.Status);

            // Attachments
            Assert.Single(result.Attachments);
            Assert.Equal("doctor_note.pdf", result.Attachments[0].FileName);
            Assert.Equal("/storage/requests/doctor_note.pdf", result.Attachments[0].StorageUrl);

            // Prior Requests History
            Assert.Single(result.PreviousRequests);
            Assert.Equal("Hardware Replacement", result.PreviousRequests[0].RequestType);
            Assert.Equal("Approved", result.PreviousRequests[0].Status);
        }
    }

    [Fact]
    public async Task GetRequestDetail_NonExistent_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetRequestDetailQueryHandler(new GenericRepository<Request>(context));
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                handler.Handle(new GetRequestDetailQuery(9999), CancellationToken.None));
        }
    }

    [Fact]
    public async Task Controller_GetRequestDetail_ReturnsOkWhenFound()
    {
        var mockDto = new RequestDetailDto(
            1, 2, "Test Employee", "CODE", "Dept", "Leave", "Leave", DateTime.UtcNow, null, null, null, null, "Pending", null,
            new RequestReviewNoteDto("Manager", null, null, "Pending", null, null),
            new RequestReviewNoteDto("HR", null, null, "Pending", null, null),
            new List<RequestAttachmentDto>(),
            new List<PreviousRequestSummaryDto>()
        );

        var fakeMediator = new FakeMediator(mockDto);
        var controller = new RequestsController(fakeMediator);

        var actionResult = await controller.GetRequestDetail(1, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(mockDto, okResult.Value);
    }

    [Fact]
    public async Task Controller_GetRequestDetail_ReturnsNotFoundWhenMissing()
    {
        var fakeMediator = new FakeMediatorWithException(new KeyNotFoundException("Not found"));
        var controller = new RequestsController(fakeMediator);

        var actionResult = await controller.GetRequestDetail(999, CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    private class FakeMediator : ISender
    {
        private readonly object _result;
        public FakeMediator(object result) => _result = result;
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) => Task.FromResult((TResponse)_result);
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => Task.FromResult<object?>(_result);
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeMediatorWithException : ISender
    {
        private readonly Exception _exception;
        public FakeMediatorWithException(Exception exception) => _exception = exception;
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) => throw _exception;
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw _exception;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw _exception;
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw _exception;
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw _exception;
    }
}
