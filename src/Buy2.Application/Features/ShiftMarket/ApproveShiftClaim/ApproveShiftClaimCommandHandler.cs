using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.ShiftMarket;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.ShiftMarket.ApproveShiftClaim;

public class ApproveShiftClaimCommandHandler : IRequestHandler<ApproveShiftClaimCommand, ApproveShiftClaimResponseDto>
{
    private readonly IRepository<ShiftClaim> _shiftClaimRepository;
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<Request> _requestRepository;
    private readonly IRepository<Notification> _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveShiftClaimCommandHandler(
        IRepository<ShiftClaim> shiftClaimRepository,
        IRepository<ShiftEntity> shiftRepository,
        IRepository<Employee> employeeRepository,
        IRepository<Request> requestRepository,
        IRepository<Notification> notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _shiftClaimRepository = shiftClaimRepository;
        _shiftRepository = shiftRepository;
        _employeeRepository = employeeRepository;
        _requestRepository = requestRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApproveShiftClaimResponseDto> Handle(
        ApproveShiftClaimCommand request,
        CancellationToken cancellationToken)
    {
        var claim = await ValidateAndGetClaimAsync(request.ClaimId, cancellationToken);
        var shift = await ValidateAndGetShiftAsync(claim.ShiftId, claim.EmployeeId, cancellationToken);
        var employee = await LoadEmployeeAsync(claim.EmployeeId, cancellationToken);

        var projectedOvertimeHours = await CalculateProjectedOvertimeAsync(shift, employee, cancellationToken);

        return await ExecuteApprovalTransactionAsync(claim, shift, employee, projectedOvertimeHours, cancellationToken);
    }

    private async Task<ShiftClaim> ValidateAndGetClaimAsync(int claimId, CancellationToken cancellationToken)
    {
        var claim = await _shiftClaimRepository.GetByIdAsync(claimId, cancellationToken);
        if (claim is null)
        {
            throw new KeyNotFoundException($"Shift claim with ID {claimId} not found.");
        }

        if (IsClaimResolved(claim.Status))
        {
            throw new InvalidOperationException($"Shift claim with ID {claimId} is already {claim.Status}.");
        }

        return claim;
    }

    private static bool IsClaimResolved(string? status)
    {
        return string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ShiftEntity> ValidateAndGetShiftAsync(int shiftId, int employeeId, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetByIdAsync(shiftId, cancellationToken);
        if (shift is null)
        {
            throw new KeyNotFoundException($"Shift with ID {shiftId} not found.");
        }

        ValidateShiftNotAssigned(shift, employeeId);
        return shift;
    }

    private static void ValidateShiftNotAssigned(ShiftEntity shift, int employeeId)
    {
        if (!shift.EmployeeId.HasValue)
        {
            return;
        }

        if (shift.EmployeeId.Value != employeeId)
        {
            throw new InvalidOperationException($"Shift with ID {shift.Id} is already assigned to someone else.");
        }

        throw new InvalidOperationException($"Shift with ID {shift.Id} is already assigned to this employee.");
    }

    private async Task<Employee> LoadEmployeeAsync(int employeeId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.Query(true)
            .Include(e => e.PayrollProfile)
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);

        if (employee is null)
        {
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found.");
        }

        return employee;
    }

    private async Task<decimal> CalculateProjectedOvertimeAsync(
        ShiftEntity shift,
        Employee employee,
        CancellationToken cancellationToken)
    {
        var assignedShifts = await _shiftRepository.Query(true)
            .Where(s => s.EmployeeId == employee.Id && s.Id != shift.Id)
            .ToListAsync(cancellationToken);

        var shiftDuration = (decimal)Math.Max(0, (shift.EndTime - shift.StartTime).TotalHours);
        var targetDate = DateOnly.FromDateTime(shift.StartTime.Date);
        var (weekStart, weekEnd) = GetWeekBoundary(targetDate);

        var weekShifts = assignedShifts
            .Where(s => s.StartTime >= weekStart && s.StartTime < weekEnd)
            .ToList();

        var weeklyOt = CalculateWeeklyOvertime(weekShifts, shiftDuration, employee.PayrollProfile);
        var dailyOt = CalculateDailyOvertime(weekShifts, shiftDuration, targetDate);

        return Math.Max(weeklyOt, dailyOt);
    }

    private static decimal CalculateWeeklyOvertime(
        IReadOnlyList<ShiftEntity> weekShifts,
        decimal shiftDuration,
        PayrollProfile? profile)
    {
        var existingWeeklyHours = weekShifts.Sum(s => (decimal)Math.Max(0, (s.EndTime - s.StartTime).TotalHours));
        var totalWeeklyHours = existingWeeklyHours + shiftDuration;
        var threshold = profile?.OvertimeThresholdHours > 0 ? profile.OvertimeThresholdHours : 40.0m;

        return totalWeeklyHours > threshold ? Math.Round(totalWeeklyHours - threshold, 2) : 0m;
    }

    private static decimal CalculateDailyOvertime(
        IReadOnlyList<ShiftEntity> weekShifts,
        decimal shiftDuration,
        DateOnly targetDate)
    {
        var existingDailyHours = weekShifts
            .Where(s => DateOnly.FromDateTime(s.StartTime.Date) == targetDate || DateOnly.FromDateTime(s.StartTime.UtcDateTime.Date) == targetDate)
            .Sum(s => (decimal)Math.Max(0, (s.EndTime - s.StartTime).TotalHours));

        var totalDailyHours = existingDailyHours + shiftDuration;
        return totalDailyHours > 8.0m ? Math.Round(totalDailyHours - 8.0m, 2) : 0m;
    }

    private static (DateTimeOffset WeekStart, DateTimeOffset WeekEnd) GetWeekBoundary(DateOnly targetDate)
    {
        int diff = (7 + (int)targetDate.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        var monday = targetDate.AddDays(-diff);
        var sunday = monday.AddDays(6);

        var weekStart = new DateTimeOffset(monday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var weekEnd = new DateTimeOffset(sunday.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        return (weekStart, weekEnd);
    }

    private async Task<ApproveShiftClaimResponseDto> ExecuteApprovalTransactionAsync(
        ShiftClaim claim,
        ShiftEntity shift,
        Employee employee,
        decimal projectedOvertimeHours,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var response = projectedOvertimeHours > 0
                ? await ProcessOvertimeApprovalAsync(claim, shift, employee, projectedOvertimeHours, cancellationToken)
                : await ProcessImmediateAssignmentAsync(claim, shift, employee, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<ApproveShiftClaimResponseDto> ProcessImmediateAssignmentAsync(
        ShiftClaim claim,
        ShiftEntity shift,
        Employee employee,
        CancellationToken cancellationToken)
    {
        shift.EmployeeId = employee.Id;
        shift.IsPublished = true;
        shift.Status = ShiftStatus.Published;

        claim.Status = "Approved";

        await RejectCompetingClaimsAsync(claim.ShiftId, claim.Id, cancellationToken);

        var notification = CreateAssignmentNotification(shift, employee.Id);
        await _notificationRepository.AddAsync(notification, cancellationToken);

        var employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
        return new ApproveShiftClaimResponseDto(
            ClaimId: claim.Id,
            ShiftId: shift.Id,
            EmployeeId: employee.Id,
            EmployeeName: employeeName,
            ClaimStatus: "Approved",
            AssignmentOutcome: "AssignedImmediately",
            ProjectedOvertimeHours: 0m,
            RequiresHrApproval: false,
            RemainingHeadcount: 0,
            PostingStatus: "Covered",
            Message: "Shift claim approved and employee assigned immediately."
        );
    }

    private async Task RejectCompetingClaimsAsync(int shiftId, int approvedClaimId, CancellationToken cancellationToken)
    {
        var otherClaims = await _shiftClaimRepository.Query(false)
            .Where(c => c.ShiftId == shiftId && c.Id != approvedClaimId)
            .ToListAsync(cancellationToken);

        foreach (var competing in otherClaims)
        {
            if (IsPendingStatus(competing.Status))
            {
                competing.Status = "Rejected";
            }
        }
    }

    private static bool IsPendingStatus(string? status)
    {
        return string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, "Pending HR Approval", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ApproveShiftClaimResponseDto> ProcessOvertimeApprovalAsync(
        ShiftClaim claim,
        ShiftEntity shift,
        Employee employee,
        decimal projectedOvertimeHours,
        CancellationToken cancellationToken)
    {
        claim.Status = "Pending HR Approval";
        shift.Status = ShiftStatus.PendingHrApproval;

        var hrRequest = CreateHrApprovalRequest(claim, shift, employee.Id, projectedOvertimeHours);
        await _requestRepository.AddAsync(hrRequest, cancellationToken);

        await CreateOvertimeNotificationsAsync(shift, employee, projectedOvertimeHours, cancellationToken);

        var employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
        return new ApproveShiftClaimResponseDto(
            ClaimId: claim.Id,
            ShiftId: shift.Id,
            EmployeeId: employee.Id,
            EmployeeName: employeeName,
            ClaimStatus: "Pending HR Approval",
            AssignmentOutcome: "AwaitingHrApproval",
            ProjectedOvertimeHours: projectedOvertimeHours,
            RequiresHrApproval: true,
            RemainingHeadcount: 1,
            PostingStatus: "Pending",
            Message: "Shift claim requires HR approval due to projected overtime."
        );
    }

    private static Request CreateHrApprovalRequest(
        ShiftClaim claim,
        ShiftEntity shift,
        int employeeId,
        decimal projectedOvertimeHours)
    {
        var reason = string.IsNullOrWhiteSpace(claim.OvertimeJustification)
            ? $"Overtime approval required: {projectedOvertimeHours} projected OT hours."
            : $"{claim.OvertimeJustification} (Projected OT: {projectedOvertimeHours}h)";

        return new Request
        {
            EmployeeId = employeeId,
            RequestTypeId = 1,
            StartDate = shift.StartTime.UtcDateTime,
            EndDate = shift.EndTime.UtcDateTime,
            Reason = reason,
            Status = "Pending HR Approval",
            SubmittedAt = DateTime.UtcNow
        };
    }

    private async Task CreateOvertimeNotificationsAsync(
        ShiftEntity shift,
        Employee employee,
        decimal projectedOvertimeHours,
        CancellationToken cancellationToken)
    {
        var employeeNotification = new Notification
        {
            EmployeeId = employee.Id,
            Title = "Shift Claim Pending HR Approval",
            Body = $"Your claim for shift #{shift.Id} incurs {projectedOvertimeHours}h overtime and awaits HR review.",
            Type = "ShiftClaimPendingHrApproval",
            ReferenceId = shift.Id.ToString(),
            ReferenceType = "Shift",
            IsRead = false
        };
        await _notificationRepository.AddAsync(employeeNotification, cancellationToken);

        if (employee.DirectManagerId.HasValue)
        {
            var managerNotification = new Notification
            {
                EmployeeId = employee.DirectManagerId.Value,
                Title = "Shift Claim Awaiting HR Review",
                Body = $"Claim for shift #{shift.Id} by {employee.FirstName} {employee.LastName} incurs {projectedOvertimeHours}h overtime and awaits HR review.",
                Type = "ShiftClaimPendingHrApproval",
                ReferenceId = shift.Id.ToString(),
                ReferenceType = "Shift",
                IsRead = false
            };
            await _notificationRepository.AddAsync(managerNotification, cancellationToken);
        }
    }

    private static Notification CreateAssignmentNotification(ShiftEntity shift, int employeeId)
    {
        var dateStr = DateOnly.FromDateTime(shift.StartTime.Date).ToString("yyyy-MM-dd");
        return new Notification
        {
            EmployeeId = employeeId,
            Title = "Shift Claim Approved",
            Body = $"Your claim for shift on {dateStr} has been approved and assigned.",
            Type = "ShiftClaimApproved",
            ReferenceId = shift.Id.ToString(),
            ReferenceType = "Shift",
            IsRead = false
        };
    }
}
