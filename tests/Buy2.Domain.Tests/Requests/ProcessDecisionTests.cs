using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.Requests.ProcessDecision;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Buy2.Domain.Tests.Requests;

public class ProcessDecisionTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task ProcessDecision_ManagerApprove_KeepsOverallPending_WhenHrPending()
    {
        var dbName = Guid.NewGuid().ToString();
        int requestId, managerId;

        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
            var manager = new Employee { FirstName = "Bob", LastName = "Manager", Email = "bob@example.com" };
            var reqType = new RequestType { Name = "Remote Work", Category = "General" };

            context.Employees.AddRange(emp, manager);
            context.RequestTypes.Add(reqType);
            await context.SaveChangesAsync();

            var req = new Request
            {
                EmployeeId = emp.Id,
                RequestTypeId = reqType.Id,
                ManagerId = manager.Id,
                Status = "Pending",
                ManagerStatus = "Pending",
                HrStatus = "Pending",
                SubmittedAt = DateTime.UtcNow
            };
            context.Requests.Add(req);
            await context.SaveChangesAsync();

            requestId = req.Id;
            managerId = manager.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new ProcessRequestDecisionCommandHandler(
                new GenericRepository<Request>(context),
                new GenericRepository<Notification>(context),
                new UnitOfWork(context)
            );

            var cmd = new ProcessRequestDecisionCommand(
                RequestId: requestId,
                Tier: "Manager",
                Decision: "Approved",
                Comment: "All tasks covered.",
                RejectionReason: null,
                ReviewerId: managerId
            );

            var result = await handler.Handle(cmd, CancellationToken.None);

            Assert.Equal("Pending", result.OverallStatus);
            Assert.Equal("Approved", result.ManagerStatus);
            Assert.Equal("Pending", result.HrStatus);
            Assert.Equal("All tasks covered.", result.ManagerComment);
            Assert.NotNull(result.ManagerDecisionAt);
            Assert.Null(result.ResolvedAt);

            var notification = await context.Notifications.FirstOrDefaultAsync(n => n.ReferenceId == requestId.ToString());
            Assert.NotNull(notification);
            Assert.Equal("REQUEST_REVIEW", notification.Type);
        }
    }

    [Fact]
    public async Task ProcessDecision_BothApprove_TransitionsToApproved()
    {
        var dbName = Guid.NewGuid().ToString();
        int requestId;

        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
            var reqType = new RequestType { Name = "Leave", Category = "Leave" };
            context.Employees.Add(emp);
            context.RequestTypes.Add(reqType);
            await context.SaveChangesAsync();

            var req = new Request
            {
                EmployeeId = emp.Id,
                RequestTypeId = reqType.Id,
                Status = "Pending",
                ManagerStatus = "Approved",
                HrStatus = "Pending",
                SubmittedAt = DateTime.UtcNow
            };
            context.Requests.Add(req);
            await context.SaveChangesAsync();
            requestId = req.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new ProcessRequestDecisionCommandHandler(
                new GenericRepository<Request>(context),
                new GenericRepository<Notification>(context),
                new UnitOfWork(context)
            );

            var cmd = new ProcessRequestDecisionCommand(
                RequestId: requestId,
                Tier: "HR",
                Decision: "Approved",
                Comment: "Policy verified.",
                RejectionReason: null
            );

            var result = await handler.Handle(cmd, CancellationToken.None);

            Assert.Equal("Approved", result.OverallStatus);
            Assert.Equal("Approved", result.ManagerStatus);
            Assert.Equal("Approved", result.HrStatus);
            Assert.NotNull(result.ResolvedAt);
        }
    }

    [Fact]
    public async Task ProcessDecision_EitherRejects_TransitionsToRejected()
    {
        var dbName = Guid.NewGuid().ToString();
        int requestId;

        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
            var reqType = new RequestType { Name = "Leave", Category = "Leave" };
            context.Employees.Add(emp);
            context.RequestTypes.Add(reqType);
            await context.SaveChangesAsync();

            var req = new Request
            {
                EmployeeId = emp.Id,
                RequestTypeId = reqType.Id,
                Status = "Pending",
                ManagerStatus = "Pending",
                HrStatus = "Pending",
                SubmittedAt = DateTime.UtcNow
            };
            context.Requests.Add(req);
            await context.SaveChangesAsync();
            requestId = req.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new ProcessRequestDecisionCommandHandler(
                new GenericRepository<Request>(context),
                new GenericRepository<Notification>(context),
                new UnitOfWork(context)
            );

            var cmd = new ProcessRequestDecisionCommand(
                RequestId: requestId,
                Tier: "Manager",
                Decision: "Rejected",
                Comment: "Insufficient project coverage.",
                RejectionReason: "Coverage Issue"
            );

            var result = await handler.Handle(cmd, CancellationToken.None);

            Assert.Equal("Rejected", result.OverallStatus);
            Assert.Equal("Rejected", result.ManagerStatus);
            Assert.Equal("Coverage Issue", result.RejectionReason);
            Assert.NotNull(result.ResolvedAt);
        }
    }

    [Fact]
    public async Task ProcessDecision_RejectionWithoutReasonOrComment_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();
        int requestId;

        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
            var reqType = new RequestType { Name = "Leave", Category = "Leave" };
            context.Employees.Add(emp);
            context.RequestTypes.Add(reqType);
            await context.SaveChangesAsync();

            var req = new Request
            {
                EmployeeId = emp.Id,
                RequestTypeId = reqType.Id,
                Status = "Pending",
                SubmittedAt = DateTime.UtcNow
            };
            context.Requests.Add(req);
            await context.SaveChangesAsync();
            requestId = req.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new ProcessRequestDecisionCommandHandler(
                new GenericRepository<Request>(context),
                new GenericRepository<Notification>(context),
                new UnitOfWork(context)
            );

            var cmd = new ProcessRequestDecisionCommand(
                RequestId: requestId,
                Tier: "Manager",
                Decision: "Rejected",
                Comment: "",
                RejectionReason: ""
            );

            await Assert.ThrowsAsync<ValidationException>(() =>
                handler.Handle(cmd, CancellationToken.None));
        }
    }

    [Fact]
    public void ProcessRequestDecisionCommandValidator_ValidCommand_Passes()
    {
        var validator = new ProcessRequestDecisionCommandValidator();
        var cmd = new ProcessRequestDecisionCommand(1, "HR", "Approved", "All verified", null);
        var result = validator.Validate(cmd);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ProcessRequestDecisionCommandValidator_RejectionWithoutReason_Fails()
    {
        var validator = new ProcessRequestDecisionCommandValidator();
        var cmd = new ProcessRequestDecisionCommand(1, "Manager", "Rejected", null, null);
        var result = validator.Validate(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Comment");
        Assert.Contains(result.Errors, e => e.PropertyName == "RejectionReason");
    }
}
