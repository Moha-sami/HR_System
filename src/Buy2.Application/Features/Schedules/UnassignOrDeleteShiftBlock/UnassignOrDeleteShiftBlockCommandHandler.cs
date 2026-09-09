using System.ComponentModel.DataAnnotations;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Schedules;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Schedules.UnassignOrDeleteShiftBlock;

public class UnassignOrDeleteShiftBlockCommandHandler : IRequestHandler<UnassignOrDeleteShiftBlockCommand, UnassignOrDeleteShiftBlockResponseDto>
{
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<SiteOperationalHour>? _operationalHourRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UnassignOrDeleteShiftBlockCommandHandler(
        IRepository<ShiftEntity> shiftRepository,
        IRepository<Employee> employeeRepository,
        IRepository<Site> siteRepository,
        IUnitOfWork unitOfWork)
        : this(shiftRepository, employeeRepository, siteRepository, unitOfWork, null)
    {
    }

    public UnassignOrDeleteShiftBlockCommandHandler(
        IRepository<ShiftEntity> shiftRepository,
        IRepository<Employee> employeeRepository,
        IRepository<Site> siteRepository,
        IUnitOfWork unitOfWork,
        IRepository<SiteOperationalHour>? operationalHourRepository)
    {
        _shiftRepository = shiftRepository;
        _employeeRepository = employeeRepository;
        _siteRepository = siteRepository;
        _unitOfWork = unitOfWork;
        _operationalHourRepository = operationalHourRepository;
    }

    public async Task<UnassignOrDeleteShiftBlockResponseDto> Handle(
        UnassignOrDeleteShiftBlockCommand request,
        CancellationToken cancellationToken)
    {
        var shift = await GetTrackedShiftAsync(request.ShiftBlockId, cancellationToken);

        ValidatePublishedDeletion(shift, request);

        var siteId = shift.SiteId;
        var targetDate = DateOnly.FromDateTime(shift.StartTime.Date);

        if (request.Action == ShiftBlockRemovalAction.DeleteBlock)
        {
            return await ExecuteDeleteAsync(shift, siteId, targetDate, cancellationToken);
        }

        return await ExecuteUnassignAsync(shift, siteId, targetDate, cancellationToken);
    }

    private async Task<ShiftEntity> GetTrackedShiftAsync(int shiftBlockId, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.Query(asNoTracking: false)
            .Include(s => s.JobRole)
            .FirstOrDefaultAsync(s => s.Id == shiftBlockId, cancellationToken);

        if (shift == null)
        {
            shift = await _shiftRepository.GetByIdAsync(shiftBlockId, cancellationToken);
        }

        if (shift == null)
        {
            throw new KeyNotFoundException($"Shift block with ID {shiftBlockId} not found.");
        }

        return shift;
    }

    private static void ValidatePublishedDeletion(ShiftEntity shift, UnassignOrDeleteShiftBlockCommand request)
    {
        if (request.Action == ShiftBlockRemovalAction.DeleteBlock && shift.IsPublished && !request.ConfirmPublishedDeletion)
        {
            throw new ValidationException("Cannot delete a published shift block without explicit confirmation (ConfirmPublishedDeletion).");
        }
    }

    private async Task<UnassignOrDeleteShiftBlockResponseDto> ExecuteUnassignAsync(
        ShiftEntity shift,
        int siteId,
        DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        if (shift.EmployeeId != null)
        {
            shift.EmployeeId = null;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var newCost = await CalculateDailyLaborCostAsync(siteId, targetDate, cancellationToken);
        var newCoverageStatus = await CalculateSiteCoverageStatusAsync(siteId, targetDate, cancellationToken);
        var updatedBlock = BuildUnassignedBlockDto(shift);

        return new UnassignOrDeleteShiftBlockResponseDto(
            ShiftBlockId: shift.Id,
            ActionTaken: ShiftBlockRemovalAction.UnassignOnly,
            IsDeleted: false,
            UpdatedBlock: updatedBlock,
            UpdatedDailyLaborCost: newCost,
            SiteCoverageStatus: newCoverageStatus
        );
    }

    private async Task<UnassignOrDeleteShiftBlockResponseDto> ExecuteDeleteAsync(
        ShiftEntity shift,
        int siteId,
        DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        _shiftRepository.Delete(shift);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var newCost = await CalculateDailyLaborCostAsync(siteId, targetDate, cancellationToken);
        var newCoverageStatus = await CalculateSiteCoverageStatusAsync(siteId, targetDate, cancellationToken);

        return new UnassignOrDeleteShiftBlockResponseDto(
            ShiftBlockId: shift.Id,
            ActionTaken: ShiftBlockRemovalAction.DeleteBlock,
            IsDeleted: true,
            UpdatedBlock: null,
            UpdatedDailyLaborCost: newCost,
            SiteCoverageStatus: newCoverageStatus
        );
    }

    private static DailyShiftBlockDto BuildUnassignedBlockDto(ShiftEntity shift)
    {
        var statusColor = shift.IsPublished ? "#FFA500" : "#3B82F6";

        return new DailyShiftBlockDto(
            ShiftId: shift.Id,
            SiteId: shift.SiteId,
            JobRoleId: shift.JobRoleId,
            RoleTitle: shift.JobRole?.Title ?? string.Empty,
            StartTime: shift.StartTime,
            EndTime: shift.EndTime,
            IsPublished: shift.IsPublished,
            EmployeeId: null,
            EmployeeName: null,
            EmployeeAvatarUrl: null,
            StatusColorCode: statusColor
        );
    }

    private async Task<decimal> CalculateDailyLaborCostAsync(
        int siteId,
        DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        var targetDateShifts = await GetTargetDateShiftsAsync(siteId, targetDate, cancellationToken);
        var employeeIds = targetDateShifts
            .Where(s => s.EmployeeId.HasValue)
            .Select(s => s.EmployeeId!.Value);

        var employees = await GetEmployeesAsync(employeeIds, cancellationToken);
        return CalculateTotalLaborCost(targetDateShifts, employees);
    }

    private async Task<WeekDayCalendarStatus> CalculateSiteCoverageStatusAsync(
        int siteId,
        DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        var isDayOff = await IsSiteDayOffAsync(siteId, targetDate.DayOfWeek, cancellationToken);
        if (isDayOff)
        {
            return WeekDayCalendarStatus.DimmedDayOff;
        }

        var targetDateShifts = await GetTargetDateShiftsAsync(siteId, targetDate, cancellationToken);
        if (targetDateShifts.Count == 0)
        {
            return WeekDayCalendarStatus.NoAllocations;
        }

        var employeeIds = targetDateShifts
            .Where(s => s.EmployeeId.HasValue)
            .Select(s => s.EmployeeId!.Value);

        var employees = await GetEmployeesAsync(employeeIds, cancellationToken);
        return DetermineDayStatus(targetDateShifts, employees);
    }

    private async Task<List<ShiftEntity>> GetTargetDateShiftsAsync(
        int siteId,
        DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        var dayStart = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(-1);
        var dayEnd = dayStart.AddDays(3);

        var siteShifts = await _shiftRepository.Query(true)
            .Include(s => s.JobRole)
            .Where(s => s.SiteId == siteId && s.StartTime >= dayStart && s.StartTime < dayEnd)
            .ToListAsync(cancellationToken);

        return siteShifts
            .Where(s => DateOnly.FromDateTime(s.StartTime.Date) == targetDate || DateOnly.FromDateTime(s.StartTime.UtcDateTime.Date) == targetDate)
            .ToList();
    }

    private async Task<Dictionary<int, Employee>> GetEmployeesAsync(
        IEnumerable<int> employeeIds,
        CancellationToken cancellationToken)
    {
        var ids = employeeIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, Employee>();
        }

        return await _employeeRepository.Query(true)
            .Include(e => e.PayrollProfile)
            .Where(e => ids.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);
    }

    private async Task<bool> IsSiteDayOffAsync(
        int siteId,
        DayOfWeek dayOfWeek,
        CancellationToken cancellationToken)
    {
        if (_operationalHourRepository != null)
        {
            var opHour = await _operationalHourRepository.Query(true)
                .FirstOrDefaultAsync(o => o.SiteId == siteId && o.DayOfWeek == dayOfWeek, cancellationToken);
            if (opHour != null)
            {
                return !opHour.IsOpen;
            }
        }

        var site = await _siteRepository.Query(true)
            .Include(s => s.OperationalHours)
            .FirstOrDefaultAsync(s => s.Id == siteId, cancellationToken);

        var hourRecord = site?.OperationalHours?.FirstOrDefault(o => o.DayOfWeek == dayOfWeek);
        return hourRecord != null && !hourRecord.IsOpen;
    }

    private static WeekDayCalendarStatus DetermineDayStatus(
        IReadOnlyList<ShiftEntity> shifts,
        IReadOnlyDictionary<int, Employee> employees)
    {
        if (HasOvertimeOrMisallocation(shifts, employees))
        {
            return WeekDayCalendarStatus.OvertimeOrMisallocation;
        }

        if (shifts.Any(s => s.EmployeeId == null || !s.IsPublished))
        {
            return WeekDayCalendarStatus.MissingResourcesOrUnpublished;
        }

        return WeekDayCalendarStatus.CoveredAndPublished;
    }

    private static bool HasOvertimeOrMisallocation(
        IReadOnlyList<ShiftEntity> shifts,
        IReadOnlyDictionary<int, Employee> employees)
    {
        return shifts.Any(s =>
            (s.EndTime - s.StartTime).TotalHours > 8.0 ||
            (s.EmployeeId != null &&
             employees.TryGetValue(s.EmployeeId.Value, out var emp) &&
             emp.JobRoleId != s.JobRoleId));
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
}
