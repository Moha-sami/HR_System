using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Schedules;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Schedules.PreflightCopyShifts;

public class PreflightCopyShiftsQueryHandler : IRequestHandler<PreflightCopyShiftsQuery, PreflightCopyShiftsResponseDto>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public PreflightCopyShiftsQueryHandler(
        IRepository<Site> siteRepository,
        IRepository<ShiftEntity> shiftRepository,
        IRepository<Employee> employeeRepository)
    {
        _siteRepository = siteRepository;
        _shiftRepository = shiftRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<PreflightCopyShiftsResponseDto> Handle(
        PreflightCopyShiftsQuery request,
        CancellationToken cancellationToken)
    {
        await ValidateSiteExistsAsync(request.SiteId, cancellationToken);
        ValidateInputTargets(request);

        var targetDates = ResolveTargetDates(request);
        if (targetDates.Count == 0)
        {
            return BuildEmptyResponse(request);
        }

        var shifts = await LoadShiftsInRangeAsync(request.SiteId, targetDates, cancellationToken);
        var employees = await LoadEmployeesAsync(shifts, cancellationToken);

        return BuildResponse(request, targetDates, shifts, employees);
    }

    private async Task ValidateSiteExistsAsync(int siteId, CancellationToken cancellationToken)
    {
        var siteExists = await _siteRepository.AnyAsync(s => s.Id == siteId, cancellationToken);
        if (!siteExists)
        {
            throw new KeyNotFoundException($"Site with ID {siteId} not found.");
        }
    }

    private static void ValidateInputTargets(PreflightCopyShiftsQuery request)
    {
        var hasDates = request.TargetDates != null && request.TargetDates.Count > 0;
        var hasRecurring = request.RecurringDays != null && request.RecurringDays.Count > 0;

        if (!hasDates && !hasRecurring)
        {
            throw new ArgumentException("At least one target date or recurring pattern must be specified.");
        }
    }

    private static List<DateOnly> ResolveTargetDates(PreflightCopyShiftsQuery request)
    {
        var dates = new HashSet<DateOnly>();

        AddExplicitDates(dates, request.TargetDates, request.SourceDate);
        AddRecurringDates(dates, request);

        return dates.OrderBy(d => d).ToList();
    }

    private static void AddExplicitDates(
        HashSet<DateOnly> set,
        IReadOnlyList<DateOnly>? targetDates,
        DateOnly sourceDate)
    {
        if (targetDates == null)
        {
            return;
        }

        foreach (var date in targetDates)
        {
            if (date != sourceDate)
            {
                set.Add(date);
            }
        }
    }

    private static void AddRecurringDates(HashSet<DateOnly> set, PreflightCopyShiftsQuery request)
    {
        if (request.RecurringDays == null || request.RecurringDays.Count == 0)
        {
            return;
        }

        var weekCount = Math.Max(1, request.WeekCount.GetValueOrDefault(1));
        var totalDays = weekCount * 7;

        for (int dayOffset = 1; dayOffset <= totalDays; dayOffset++)
        {
            var calendarDate = request.SourceDate.AddDays(dayOffset);
            if (request.RecurringDays.Contains(calendarDate.DayOfWeek))
            {
                set.Add(calendarDate);
            }
        }
    }

    private async Task<List<ShiftEntity>> LoadShiftsInRangeAsync(
        int siteId,
        IReadOnlyList<DateOnly> sortedDates,
        CancellationToken cancellationToken)
    {
        var minDate = sortedDates[0];
        var maxDate = sortedDates[^1];

        var queryStart = new DateTimeOffset(minDate.AddDays(-1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var queryEnd = new DateTimeOffset(maxDate.AddDays(2).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        return await _shiftRepository.Query(true)
            .Include(s => s.ShiftTemplate)
            .Include(s => s.JobRole)
            .Where(s => s.SiteId == siteId && s.StartTime >= queryStart && s.StartTime < queryEnd)
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<int, Employee>> LoadEmployeesAsync(
        IReadOnlyList<ShiftEntity> shifts,
        CancellationToken cancellationToken)
    {
        var employeeIds = shifts
            .Where(s => s.EmployeeId.HasValue)
            .Select(s => s.EmployeeId!.Value)
            .Distinct()
            .ToList();

        if (employeeIds.Count == 0)
        {
            return new Dictionary<int, Employee>();
        }

        return await _employeeRepository.Query(true)
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);
    }

    private static bool IsShiftOnDate(ShiftEntity shift, DateOnly date)
    {
        return DateOnly.FromDateTime(shift.StartTime.Date) == date ||
               DateOnly.FromDateTime(shift.StartTime.UtcDateTime.Date) == date;
    }

    private static string ResolveShiftName(ShiftEntity shift)
    {
        if (shift.ShiftTemplate != null && !string.IsNullOrWhiteSpace(shift.ShiftTemplate.Name))
        {
            return shift.ShiftTemplate.Name;
        }

        if (shift.JobRole != null && !string.IsNullOrWhiteSpace(shift.JobRole.Title))
        {
            return shift.JobRole.Title;
        }

        return $"Shift #{shift.Id}";
    }

    private static ShiftConflictSummaryDto MapConflictSummary(
        ShiftEntity shift,
        IReadOnlyDictionary<int, Employee> employees)
    {
        var shiftName = ResolveShiftName(shift);
        var employeeName = ResolveEmployeeName(shift, employees);

        return new ShiftConflictSummaryDto(
            ShiftId: shift.Id,
            ShiftName: shiftName,
            StartTime: shift.StartTime.TimeOfDay,
            EndTime: shift.EndTime.TimeOfDay,
            JobRoleTitle: shift.JobRole?.Title,
            EmployeeName: employeeName
        );
    }

    private static string? ResolveEmployeeName(
        ShiftEntity shift,
        IReadOnlyDictionary<int, Employee> employees)
    {
        if (!shift.EmployeeId.HasValue || !employees.TryGetValue(shift.EmployeeId.Value, out var employee))
        {
            return null;
        }

        var fullName = $"{employee.FirstName} {employee.LastName}".Trim();
        return string.IsNullOrEmpty(fullName) ? null : fullName;
    }

    private static ConflictingDateDto BuildConflictingDateDto(
        DateOnly date,
        IReadOnlyList<ShiftEntity> shiftsOnDate,
        IReadOnlyDictionary<int, Employee> employees)
    {
        var summaries = shiftsOnDate
            .OrderBy(s => s.StartTime)
            .Select(s => MapConflictSummary(s, employees))
            .ToList();

        var shiftNames = summaries
            .Select(s => s.ShiftName)
            .ToList();

        return new ConflictingDateDto(
            Date: date,
            ShiftCount: summaries.Count,
            ShiftNames: shiftNames,
            ExistingShifts: summaries
        );
    }

    private static PreflightCopyShiftsResponseDto BuildResponse(
        PreflightCopyShiftsQuery request,
        IReadOnlyList<DateOnly> targetDates,
        IReadOnlyList<ShiftEntity> shifts,
        IReadOnlyDictionary<int, Employee> employees)
    {
        var conflictFreeDates = new List<DateOnly>();
        var conflictingDates = new List<ConflictingDateDto>();

        foreach (var date in targetDates)
        {
            var shiftsOnDate = shifts.Where(s => IsShiftOnDate(s, date)).ToList();
            if (shiftsOnDate.Count == 0)
            {
                conflictFreeDates.Add(date);
            }
            else
            {
                conflictingDates.Add(BuildConflictingDateDto(date, shiftsOnDate, employees));
            }
        }

        return new PreflightCopyShiftsResponseDto(
            SiteId: request.SiteId,
            SourceDate: request.SourceDate,
            TotalTargetDates: targetDates.Count,
            ConflictFreeDates: conflictFreeDates,
            ConflictingDates: conflictingDates,
            HasConflicts: conflictingDates.Count > 0
        );
    }

    private static PreflightCopyShiftsResponseDto BuildEmptyResponse(PreflightCopyShiftsQuery request)
    {
        return new PreflightCopyShiftsResponseDto(
            SiteId: request.SiteId,
            SourceDate: request.SourceDate,
            TotalTargetDates: 0,
            ConflictFreeDates: new List<DateOnly>(),
            ConflictingDates: new List<ConflictingDateDto>(),
            HasConflicts: false
        );
    }
}
