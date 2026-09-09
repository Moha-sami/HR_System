using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Schedules;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Schedules.AssignEmployeeToShift;

public class AssignEmployeeToShiftCommandHandler : IRequestHandler<AssignEmployeeToShiftCommand, AssignEmployeeToShiftResponseDto>
{
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignEmployeeToShiftCommandHandler(
        IRepository<ShiftEntity> shiftRepository,
        IRepository<Employee> employeeRepository,
        IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AssignEmployeeToShiftResponseDto> Handle(
        AssignEmployeeToShiftCommand request,
        CancellationToken cancellationToken)
    {
        var shift = await GetShiftAsync(request.ShiftBlockId, cancellationToken);
        var employee = await GetEmployeeAsync(request.EmployeeId, cancellationToken);

        var warnings = await DetectConflictsAsync(shift, employee, cancellationToken);
        var targetDate = DateOnly.FromDateTime(shift.StartTime.Date);

        if (warnings.Count > 0 && !request.ConfirmOverride)
        {
            var currentCost = await CalculateDailyLaborCostAsync(shift.SiteId, targetDate, cancellationToken);
            return new AssignEmployeeToShiftResponseDto(
                Success: false,
                HasConflicts: true,
                Warnings: warnings,
                WasOverridden: false,
                OverrideReason: null,
                UpdatedBlock: null,
                UpdatedDailyLaborCost: currentCost
            );
        }

        return await CommitAssignmentAsync(shift, employee, warnings, request, targetDate, cancellationToken);
    }

    private async Task<ShiftEntity> GetShiftAsync(int shiftBlockId, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.Query(true)
            .Include(s => s.JobRole)
            .FirstOrDefaultAsync(s => s.Id == shiftBlockId, cancellationToken);

        if (shift == null)
        {
            throw new KeyNotFoundException($"Shift block with ID {shiftBlockId} not found.");
        }

        return shift;
    }

    private async Task<Employee> GetEmployeeAsync(int employeeId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.Query(true)
            .Include(e => e.JobRole)
            .Include(e => e.PayrollProfile)
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted && e.IsActive, cancellationToken);

        if (employee == null)
        {
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found or inactive.");
        }

        return employee;
    }

    private async Task<List<AssignmentConflictWarningDto>> DetectConflictsAsync(
        ShiftEntity shift,
        Employee employee,
        CancellationToken cancellationToken)
    {
        var warnings = new List<AssignmentConflictWarningDto>();

        var roleMismatch = CheckQualificationMismatch(shift, employee);
        if (roleMismatch != null)
        {
            warnings.Add(roleMismatch);
        }

        var overtimeRisk = await CheckOvertimeRiskAsync(shift, employee, cancellationToken);
        if (overtimeRisk != null)
        {
            warnings.Add(overtimeRisk);
        }

        return warnings;
    }

    private static AssignmentConflictWarningDto? CheckQualificationMismatch(ShiftEntity shift, Employee employee)
    {
        if (employee.JobRoleId == shift.JobRoleId)
        {
            return null;
        }

        var empRole = employee.JobRole?.Title ?? "Unknown";
        var shiftRole = shift.JobRole?.Title ?? "Unknown";

        return new AssignmentConflictWarningDto(
            AssignmentConflictType.QualificationMismatch,
            $"Employee job role '{empRole}' does not match required shift role '{shiftRole}'."
        );
    }

    private async Task<AssignmentConflictWarningDto?> CheckOvertimeRiskAsync(
        ShiftEntity shift,
        Employee employee,
        CancellationToken cancellationToken)
    {
        var shiftDuration = (decimal)(shift.EndTime - shift.StartTime).TotalHours;
        if (shiftDuration > 8.0m)
        {
            return new AssignmentConflictWarningDto(
                AssignmentConflictType.OvertimeRisk,
                "Assignment incurs overtime risk."
            );
        }

        var targetDate = DateOnly.FromDateTime(shift.StartTime.Date);
        var (weekStart, weekEnd) = GetWeekBoundary(targetDate);

        var otherShifts = await _shiftRepository.Query(true)
            .Where(s => s.EmployeeId == employee.Id && s.Id != shift.Id && s.StartTime >= weekStart && s.StartTime < weekEnd)
            .ToListAsync(cancellationToken);

        if (HasOvertimeRiskInShifts(otherShifts, targetDate, shiftDuration, employee.PayrollProfile))
        {
            return new AssignmentConflictWarningDto(
                AssignmentConflictType.OvertimeRisk,
                "Assignment incurs overtime risk."
            );
        }

        return null;
    }

    private static bool HasOvertimeRiskInShifts(
        IReadOnlyList<ShiftEntity> otherShifts,
        DateOnly targetDate,
        decimal shiftDuration,
        PayrollProfile? payroll)
    {
        var hasSameDayShift = otherShifts.Any(s =>
            DateOnly.FromDateTime(s.StartTime.Date) == targetDate ||
            DateOnly.FromDateTime(s.StartTime.UtcDateTime.Date) == targetDate);

        if (hasSameDayShift)
        {
            return true;
        }

        var threshold = payroll?.OvertimeThresholdHours > 0
            ? payroll.OvertimeThresholdHours
            : 40m;

        var totalWeeklyHours = otherShifts.Sum(s => (decimal)(s.EndTime - s.StartTime).TotalHours) + shiftDuration;
        return totalWeeklyHours > threshold;
    }

    private async Task<AssignEmployeeToShiftResponseDto> CommitAssignmentAsync(
        ShiftEntity shift,
        Employee employee,
        List<AssignmentConflictWarningDto> warnings,
        AssignEmployeeToShiftCommand request,
        DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        var trackedShift = await _shiftRepository.GetByIdAsync(shift.Id, cancellationToken);
        if (trackedShift == null)
        {
            throw new KeyNotFoundException($"Shift block with ID {shift.Id} not found.");
        }

        trackedShift.EmployeeId = employee.Id;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var newLaborCost = await CalculateDailyLaborCostAsync(shift.SiteId, targetDate, cancellationToken);
        var hasConflicts = warnings.Count > 0;
        var statusColor = DetermineStatusColorCode(hasConflicts, shift.IsPublished);
        var updatedBlock = BuildDailyShiftBlockDto(shift, employee, statusColor);

        return new AssignEmployeeToShiftResponseDto(
            Success: true,
            HasConflicts: hasConflicts,
            Warnings: warnings,
            WasOverridden: hasConflicts && request.ConfirmOverride,
            OverrideReason: request.OverrideReason,
            UpdatedBlock: updatedBlock,
            UpdatedDailyLaborCost: newLaborCost
        );
    }

    private static string DetermineStatusColorCode(bool hasConflicts, bool isPublished)
    {
        if (hasConflicts)
        {
            return "#EF4444";
        }

        if (!isPublished)
        {
            return "#3B82F6";
        }

        return "#10B981";
    }

    private static DailyShiftBlockDto BuildDailyShiftBlockDto(
        ShiftEntity shift,
        Employee employee,
        string statusColorCode)
    {
        var employeeName = $"{employee.FirstName} {employee.LastName}".Trim();

        return new DailyShiftBlockDto(
            ShiftId: shift.Id,
            SiteId: shift.SiteId,
            JobRoleId: shift.JobRoleId,
            RoleTitle: shift.JobRole?.Title ?? string.Empty,
            StartTime: shift.StartTime,
            EndTime: shift.EndTime,
            IsPublished: shift.IsPublished,
            EmployeeId: employee.Id,
            EmployeeName: employeeName,
            EmployeeAvatarUrl: employee.ProfilePhotoUrl,
            StatusColorCode: statusColorCode
        );
    }

    private async Task<decimal> CalculateDailyLaborCostAsync(
        int siteId,
        DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        var dayStart = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(-1);
        var dayEnd = dayStart.AddDays(3);

        var siteShifts = await _shiftRepository.Query(true)
            .Where(s => s.SiteId == siteId && s.StartTime >= dayStart && s.StartTime < dayEnd)
            .ToListAsync(cancellationToken);

        var targetDateShifts = siteShifts
            .Where(s => DateOnly.FromDateTime(s.StartTime.Date) == targetDate || DateOnly.FromDateTime(s.StartTime.UtcDateTime.Date) == targetDate)
            .ToList();

        var employeeIds = targetDateShifts
            .Where(s => s.EmployeeId.HasValue)
            .Select(s => s.EmployeeId!.Value)
            .Distinct()
            .ToList();

        if (employeeIds.Count == 0)
        {
            return 0m;
        }

        var employees = await _employeeRepository.Query(true)
            .Include(e => e.PayrollProfile)
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);

        return CalculateTotalLaborCost(targetDateShifts, employees);
    }

    private static decimal CalculateTotalLaborCost(
        IReadOnlyList<ShiftEntity> shifts,
        IReadOnlyDictionary<int, Employee> employees)
    {
        decimal totalCost = 0m;
        foreach (var shift in shifts)
        {
            if (shift.EmployeeId == null || !employees.TryGetValue(shift.EmployeeId.Value, out var emp))
            {
                continue;
            }

            var duration = (decimal)Math.Max(0, (shift.EndTime - shift.StartTime).TotalHours);
            var rate = CalculateHourlyRate(emp.PayrollProfile);
            totalCost += rate * duration;
        }

        return Math.Round(totalCost, 2);
    }

    private static decimal CalculateHourlyRate(PayrollProfile? payroll)
    {
        if (payroll == null)
        {
            return 0m;
        }

        if (string.Equals(payroll.SalaryType, "Monthly", StringComparison.OrdinalIgnoreCase) && payroll.PaymentAmount > 0)
        {
            return Math.Round(payroll.PaymentAmount / 160m, 2);
        }

        return payroll.PaymentAmount;
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
}
