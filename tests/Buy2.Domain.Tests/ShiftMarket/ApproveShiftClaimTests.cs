using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Api.Controllers;
using Buy2.Application.DTOs.ShiftMarket;
using Buy2.Application.Features.ShiftMarket.ApproveShiftClaim;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.ShiftMarket;

public class ApproveShiftClaimTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static ApproveShiftClaimCommandHandler CreateHandler(Buy2DbContext context)
    {
        return new ApproveShiftClaimCommandHandler(
            new GenericRepository<ShiftClaim>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context),
            new GenericRepository<Request>(context),
            new GenericRepository<Notification>(context),
            new UnitOfWork(context)
        );
    }

    [Fact]
    public async Task Handle_ClaimNotFound_ThrowsKeyNotFoundException()
    {
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var command = new ApproveShiftClaimCommand(999);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ClaimAlreadyApproved_ThrowsInvalidOperationException()
    {
        using var context = CreateDbContext();
        var claim = new ShiftClaim { Id = 1, ShiftId = 10, EmployeeId = 5, Status = "Approved" };
        context.ShiftClaims.Add(claim);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new ApproveShiftClaimCommand(1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("already", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ClaimAlreadyRejected_ThrowsInvalidOperationException()
    {
        using var context = CreateDbContext();
        var claim = new ShiftClaim { Id = 2, ShiftId = 10, EmployeeId = 5, Status = "Rejected" };
        context.ShiftClaims.Add(claim);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new ApproveShiftClaimCommand(2);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("already", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ShiftNotFound_ThrowsKeyNotFoundException()
    {
        using var context = CreateDbContext();
        var claim = new ShiftClaim { Id = 3, ShiftId = 999, EmployeeId = 5, Status = "Pending" };
        context.ShiftClaims.Add(claim);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new ApproveShiftClaimCommand(3);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShiftAlreadyAssignedToSomeoneElse_ThrowsInvalidOperationException()
    {
        using var context = CreateDbContext();
        var shift = new ShiftEntity
        {
            Id = 10,
            SiteId = 1,
            JobRoleId = 1,
            StartTime = new DateTimeOffset(2026, 9, 15, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 15, 16, 0, 0, TimeSpan.Zero),
            EmployeeId = 99
        };
        var claim = new ShiftClaim { Id = 4, ShiftId = 10, EmployeeId = 5, Status = "Pending" };
        context.ShiftEntities.Add(shift);
        context.ShiftClaims.Add(claim);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new ApproveShiftClaimCommand(4);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("already assigned", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_EmployeeNotFound_ThrowsKeyNotFoundException()
    {
        using var context = CreateDbContext();
        var shift = new ShiftEntity
        {
            Id = 11,
            SiteId = 1,
            JobRoleId = 1,
            StartTime = new DateTimeOffset(2026, 9, 15, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 15, 16, 0, 0, TimeSpan.Zero)
        };
        var claim = new ShiftClaim { Id = 5, ShiftId = 11, EmployeeId = 999, Status = "Pending" };
        context.ShiftEntities.Add(shift);
        context.ShiftClaims.Add(claim);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new ApproveShiftClaimCommand(5);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoOvertime_AssignsEmployeeAndApprovesClaimAndRejectsCompeting()
    {
        using var context = CreateDbContext();

        var employee = new Employee
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            DirectManagerId = 2
        };
        var shift = new ShiftEntity
        {
            Id = 100,
            SiteId = 1,
            JobRoleId = 1,
            StartTime = new DateTimeOffset(2026, 9, 15, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 15, 16, 0, 0, TimeSpan.Zero),
            IsPublished = false,
            Status = ShiftStatus.Draft
        };
        var targetClaim = new ShiftClaim
        {
            Id = 10,
            ShiftId = 100,
            EmployeeId = 1,
            Status = "Pending",
            OvertimeJustification = "No overtime"
        };
        var competingClaim1 = new ShiftClaim
        {
            Id = 11,
            ShiftId = 100,
            EmployeeId = 3,
            Status = "Pending"
        };
        var competingClaim2 = new ShiftClaim
        {
            Id = 12,
            ShiftId = 100,
            EmployeeId = 4,
            Status = "Pending HR Approval"
        };

        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        context.ShiftClaims.AddRange(targetClaim, competingClaim1, competingClaim2);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new ApproveShiftClaimCommand(10);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(10, result.ClaimId);
        Assert.Equal(100, result.ShiftId);
        Assert.Equal(1, result.EmployeeId);
        Assert.Equal("John Doe", result.EmployeeName);
        Assert.Equal("Approved", result.ClaimStatus);
        Assert.Equal("AssignedImmediately", result.AssignmentOutcome);
        Assert.Equal(0m, result.ProjectedOvertimeHours);
        Assert.False(result.RequiresHrApproval);
        Assert.Equal(0, result.RemainingHeadcount);
        Assert.Equal("Covered", result.PostingStatus);

        // Verify Shift in DB
        var dbShift = await context.ShiftEntities.FindAsync(100);
        Assert.NotNull(dbShift);
        Assert.Equal(1, dbShift.EmployeeId);
        Assert.True(dbShift.IsPublished);
        Assert.Equal(ShiftStatus.Published, dbShift.Status);

        // Verify Claims in DB
        var dbTargetClaim = await context.ShiftClaims.FindAsync(10);
        Assert.NotNull(dbTargetClaim);
        Assert.Equal("Approved", dbTargetClaim.Status);

        var dbCompeting1 = await context.ShiftClaims.FindAsync(11);
        Assert.NotNull(dbCompeting1);
        Assert.Equal("Rejected", dbCompeting1.Status);

        var dbCompeting2 = await context.ShiftClaims.FindAsync(12);
        Assert.NotNull(dbCompeting2);
        Assert.Equal("Rejected", dbCompeting2.Status);

        // Verify Notification
        var notifications = await context.Notifications.Where(n => n.EmployeeId == 1).ToListAsync();
        Assert.Single(notifications);
        Assert.Equal("ShiftClaimApproved", notifications[0].Type);

        // Verify no HR request was generated
        var hrRequests = await context.Requests.ToListAsync();
        Assert.Empty(hrRequests);
    }

    [Fact]
    public async Task Handle_WeeklyOvertimeIncurred_RoutesToPendingHrApproval()
    {
        using var context = CreateDbContext();

        var employee = new Employee
        {
            Id = 1,
            FirstName = "Jane",
            LastName = "Smith",
            DirectManagerId = 2,
            PayrollProfile = new PayrollProfile { OvertimeThresholdHours = 40.0m }
        };

        // Tuesday 2026-09-15 is in the same week (Monday 2026-09-14 to Sunday 2026-09-20)
        // Add existing 36h shifts in that week
        var existingShift = new ShiftEntity
        {
            Id = 201,
            EmployeeId = 1,
            SiteId = 1,
            JobRoleId = 1,
            StartTime = new DateTimeOffset(2026, 9, 14, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 14, 20, 0, 0, TimeSpan.Zero), // 12h
            IsPublished = true,
            Status = ShiftStatus.Published
        };
        var existingShift2 = new ShiftEntity
        {
            Id = 202,
            EmployeeId = 1,
            SiteId = 1,
            JobRoleId = 1,
            StartTime = new DateTimeOffset(2026, 9, 16, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 16, 20, 0, 0, TimeSpan.Zero), // 12h
            IsPublished = true,
            Status = ShiftStatus.Published
        };
        var existingShift3 = new ShiftEntity
        {
            Id = 203,
            EmployeeId = 1,
            SiteId = 1,
            JobRoleId = 1,
            StartTime = new DateTimeOffset(2026, 9, 17, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 17, 20, 0, 0, TimeSpan.Zero), // 12h -> Total 36h
            IsPublished = true,
            Status = ShiftStatus.Published
        };

        // New shift on 2026-09-15 of 8 hours -> Total 44h -> 4h weekly OT
        var targetShift = new ShiftEntity
        {
            Id = 300,
            SiteId = 1,
            JobRoleId = 1,
            StartTime = new DateTimeOffset(2026, 9, 15, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 15, 16, 0, 0, TimeSpan.Zero),
            IsPublished = true,
            Status = ShiftStatus.Draft
        };

        var claim = new ShiftClaim
        {
            Id = 30,
            ShiftId = 300,
            EmployeeId = 1,
            Status = "Pending",
            OvertimeJustification = "Covering sick teammate"
        };

        context.Employees.Add(employee);
        context.ShiftEntities.AddRange(existingShift, existingShift2, existingShift3, targetShift);
        context.ShiftClaims.Add(claim);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new ApproveShiftClaimCommand(30);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(30, result.ClaimId);
        Assert.Equal(300, result.ShiftId);
        Assert.Equal(1, result.EmployeeId);
        Assert.Equal("Jane Smith", result.EmployeeName);
        Assert.Equal("Pending HR Approval", result.ClaimStatus);
        Assert.Equal("AwaitingHrApproval", result.AssignmentOutcome);
        Assert.Equal(4.0m, result.ProjectedOvertimeHours);
        Assert.True(result.RequiresHrApproval);
        Assert.Equal(1, result.RemainingHeadcount);
        Assert.Equal("Pending", result.PostingStatus);

        // Verify Shift in DB
        var dbShift = await context.ShiftEntities.FindAsync(300);
        Assert.NotNull(dbShift);
        Assert.Null(dbShift.EmployeeId);
        Assert.Equal(ShiftStatus.PendingHrApproval, dbShift.Status);

        // Verify Claim in DB
        var dbClaim = await context.ShiftClaims.FindAsync(30);
        Assert.NotNull(dbClaim);
        Assert.Equal("Pending HR Approval", dbClaim.Status);

        // Verify HR Request in DB
        var hrRequests = await context.Requests.Where(r => r.EmployeeId == 1).ToListAsync();
        Assert.Single(hrRequests);
        Assert.Equal("Pending HR Approval", hrRequests[0].Status);
        Assert.Contains("Covering sick teammate", hrRequests[0].Reason);
        Assert.Contains("4", hrRequests[0].Reason);

        // Verify Notifications in DB (both employee and manager)
        var empNotifications = await context.Notifications.Where(n => n.EmployeeId == 1).ToListAsync();
        Assert.Single(empNotifications);
        Assert.Equal("ShiftClaimPendingHrApproval", empNotifications[0].Type);

        var mgrNotifications = await context.Notifications.Where(n => n.EmployeeId == 2).ToListAsync();
        Assert.Single(mgrNotifications);
        Assert.Equal("ShiftClaimPendingHrApproval", mgrNotifications[0].Type);
    }

    [Fact]
    public async Task Handle_DailyOvertimeIncurred_RoutesToPendingHrApproval()
    {
        using var context = CreateDbContext();

        var employee = new Employee
        {
            Id = 5,
            FirstName = "Alice",
            LastName = "Wonder",
            PayrollProfile = new PayrollProfile { OvertimeThresholdHours = 40.0m }
        };

        // Same day shift of 4 hours
        var existingSameDayShift = new ShiftEntity
        {
            Id = 401,
            EmployeeId = 5,
            SiteId = 1,
            JobRoleId = 1,
            StartTime = new DateTimeOffset(2026, 9, 15, 6, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 15, 10, 0, 0, TimeSpan.Zero), // 4h
            IsPublished = true,
            Status = ShiftStatus.Published
        };

        // Target shift of 6 hours on same day -> total daily 10h -> 2h daily OT (weekly only 10h < 40)
        var targetShift = new ShiftEntity
        {
            Id = 400,
            SiteId = 1,
            JobRoleId = 1,
            StartTime = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 15, 18, 0, 0, TimeSpan.Zero), // 6h
            IsPublished = true,
            Status = ShiftStatus.Draft
        };

        var claim = new ShiftClaim
        {
            Id = 40,
            ShiftId = 400,
            EmployeeId = 5,
            Status = "Pending",
            OvertimeJustification = "Extra shift"
        };

        context.Employees.Add(employee);
        context.ShiftEntities.AddRange(existingSameDayShift, targetShift);
        context.ShiftClaims.Add(claim);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new ApproveShiftClaimCommand(40);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Pending HR Approval", result.ClaimStatus);
        Assert.Equal("AwaitingHrApproval", result.AssignmentOutcome);
        Assert.Equal(2.0m, result.ProjectedOvertimeHours);
        Assert.True(result.RequiresHrApproval);
        Assert.Equal(1, result.RemainingHeadcount);
        Assert.Equal("Pending", result.PostingStatus);

        var hrRequests = await context.Requests.Where(r => r.EmployeeId == 5).ToListAsync();
        Assert.Single(hrRequests);
        Assert.Equal("Pending HR Approval", hrRequests[0].Status);
    }

    [Fact]
    public async Task Handle_CustomPayrollThreshold_UsesThresholdFromProfile()
    {
        using var context = CreateDbContext();

        var employee = new Employee
        {
            Id = 6,
            FirstName = "Bob",
            LastName = "Builder",
            PayrollProfile = new PayrollProfile { OvertimeThresholdHours = 20.0m }
        };

        // Existing shift 16 hours in week
        var existingShift = new ShiftEntity
        {
            Id = 501,
            EmployeeId = 6,
            SiteId = 1,
            JobRoleId = 1,
            StartTime = new DateTimeOffset(2026, 9, 14, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 14, 16, 0, 0, TimeSpan.Zero), // 8h
            IsPublished = true
        };
        var existingShift2 = new ShiftEntity
        {
            Id = 502,
            EmployeeId = 6,
            SiteId = 1,
            JobRoleId = 1,
            StartTime = new DateTimeOffset(2026, 9, 15, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 15, 16, 0, 0, TimeSpan.Zero), // 8h -> Total 16h
            IsPublished = true
        };

        // New shift on 2026-09-16 for 8h -> total 24h > 20h threshold -> 4h weekly OT
        var targetShift = new ShiftEntity
        {
            Id = 500,
            SiteId = 1,
            JobRoleId = 1,
            StartTime = new DateTimeOffset(2026, 9, 16, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 16, 16, 0, 0, TimeSpan.Zero), // 8h
            IsPublished = true
        };

        var claim = new ShiftClaim
        {
            Id = 50,
            ShiftId = 500,
            EmployeeId = 6,
            Status = "Pending"
        };

        context.Employees.Add(employee);
        context.ShiftEntities.AddRange(existingShift, existingShift2, targetShift);
        context.ShiftClaims.Add(claim);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new ApproveShiftClaimCommand(50);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(4.0m, result.ProjectedOvertimeHours);
        Assert.True(result.RequiresHrApproval);
        Assert.Equal("Pending HR Approval", result.ClaimStatus);
    }

    [Fact]
    public async Task Controller_Approve_ReturnsOkResult_OnSuccess()
    {
        var expected = new ApproveShiftClaimResponseDto(
            ClaimId: 1,
            ShiftId: 10,
            EmployeeId: 5,
            EmployeeName: "Test User",
            ClaimStatus: "Approved",
            AssignmentOutcome: "AssignedImmediately",
            ProjectedOvertimeHours: 0m,
            RequiresHrApproval: false,
            RemainingHeadcount: 0,
            PostingStatus: "Covered",
            Message: "Shift claim approved and employee assigned immediately."
        );

        var fakeMediator = new FakeMediator((req, ct) => Task.FromResult<object?>(expected));
        var controller = new ShiftClaimsController(fakeMediator);

        var actionResult = await controller.Approve(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApproveShiftClaimResponseDto>(okResult.Value);
        Assert.Equal(1, response.ClaimId);
        Assert.Equal("Approved", response.ClaimStatus);
        Assert.Equal("AssignedImmediately", response.AssignmentOutcome);
    }

    [Fact]
    public async Task Controller_Approve_ReturnsNotFound_WhenKeyNotFoundException()
    {
        var fakeMediator = new FakeMediator((req, ct) => throw new KeyNotFoundException("Shift claim with ID 999 not found."));
        var controller = new ShiftClaimsController(fakeMediator);

        var actionResult = await controller.Approve(999, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task Controller_Approve_ReturnsBadRequest_WhenInvalidOperationException()
    {
        var fakeMediator = new FakeMediator((req, ct) => throw new InvalidOperationException("Shift claim with ID 1 is already Approved."));
        var controller = new ShiftClaimsController(fakeMediator);

        var actionResult = await controller.Approve(1, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
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
