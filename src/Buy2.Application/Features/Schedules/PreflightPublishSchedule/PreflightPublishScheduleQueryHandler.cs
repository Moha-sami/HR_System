using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.ShiftTemplates;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Schedules.PreflightPublishSchedule;

public class PreflightPublishScheduleQueryHandler : IRequestHandler<PreflightPublishScheduleQuery, PreflightPublishScheduleResponseDto>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public PreflightPublishScheduleQueryHandler(
        IRepository<Site> siteRepository,
        IRepository<ShiftEntity> shiftRepository,
        IRepository<Employee> employeeRepository)
    {
        _siteRepository = siteRepository;
        _shiftRepository = shiftRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<PreflightPublishScheduleResponseDto> Handle(
        PreflightPublishScheduleQuery request,
        CancellationToken cancellationToken)
    {
        await ValidateSiteExistsAsync(request.SiteId, cancellationToken);
        ValidateInputTargets(request);

        var shifts = await LoadUnpublishedShiftsAsync(request, cancellationToken);
        var scannedDates = ResolveScannedDates(request, shifts);

        if (shifts.Count == 0)
        {
            return BuildEmptyResponse(request.SiteId, scannedDates);
        }

        var employees = await LoadAssignedEmployeesAsync(shifts, cancellationToken);
        var (unqualified, overtime) = await EvaluateExceptionsAsync(shifts, employees, cancellationToken);

        return BuildResponse(request.SiteId, shifts.Count, scannedDates, unqualified, overtime);
    }

    private async Task ValidateSiteExistsAsync(int siteId, CancellationToken cancellationToken)
    {
        var exists = await _siteRepository.AnyAsync(s => s.Id == siteId, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException($"Site with ID {siteId} not found.");
        }
    }

    private static void ValidateInputTargets(PreflightPublishScheduleQuery request)
    {
        var hasDates = request.TargetDates != null && request.TargetDates.Count > 0;
        if (!hasDates && !request.AllUnpublishedDays)
        {
            throw new ArgumentException("Either TargetDates or AllUnpublishedDays must be specified.");
        }
    }

    private static List<DateOnly> ResolveScannedDates(
        PreflightPublishScheduleQuery request,
        IReadOnlyList<ShiftEntity> scopedShifts)
    {
        if (request.AllUnpublishedDays)
        {
            return scopedShifts
                .Select(GetShiftDate)
                .Distinct()
                .OrderBy(d => d)
                .ToList();
        }

        return request.TargetDates!
            .Distinct()
            .OrderBy(d => d)
            .ToList();
    }

    private static DateOnly GetShiftDate(ShiftEntity shift)
    {
        return DateOnly.FromDateTime(shift.StartTime.Date);
    }

    private static string FormatTimeRange(TimeSpan start, TimeSpan end)
    {
        return $"{ShiftTimeHelper.FormatTime(start)} - {ShiftTimeHelper.FormatTime(end)}";
    }

    private async Task<List<ShiftEntity>> LoadUnpublishedShiftsAsync(
        PreflightPublishScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var query = _shiftRepository.Query(true)
            .Include(s => s.JobRole)
            .Where(s => !s.IsPublished && s.SiteId == request.SiteId);

        query = ApplyRoleFilter(query, request.TargetRoleIds);

        var shifts = await query.ToListAsync(cancellationToken);
        return FilterByTargetDates(shifts, request);
    }

    private static IQueryable<ShiftEntity> ApplyRoleFilter(
        IQueryable<ShiftEntity> query,
        IReadOnlyList<int>? roleIds)
    {
        if (roleIds != null && roleIds.Count > 0)
        {
            return query.Where(s => roleIds.Contains(s.JobRoleId));
        }

        return query;
    }

    private static List<ShiftEntity> FilterByTargetDates(
        List<ShiftEntity> shifts,
        PreflightPublishScheduleQuery request)
    {
        if (request.AllUnpublishedDays || request.TargetDates == null || request.TargetDates.Count == 0)
        {
            return shifts;
        }

        var dateSet = request.TargetDates.ToHashSet();
        return shifts.Where(s => IsShiftInDates(s, dateSet)).ToList();
    }

    private static bool IsShiftInDates(ShiftEntity shift, HashSet<DateOnly> dates)
    {
        return dates.Contains(DateOnly.FromDateTime(shift.StartTime.Date)) ||
               dates.Contains(DateOnly.FromDateTime(shift.StartTime.UtcDateTime.Date));
    }

    private async Task<Dictionary<int, Employee>> LoadAssignedEmployeesAsync(
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
            .Include(e => e.JobRole)
            .Include(e => e.PayrollProfile)
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);
    }

    private async Task<(List<UnqualifiedAssigneeExceptionDto> Unqualified, List<OvertimeViolationExceptionDto> Overtime)>
        EvaluateExceptionsAsync(
            IReadOnlyList<ShiftEntity> shifts,
            IReadOnlyDictionary<int, Employee> employees,
            CancellationToken cancellationToken)
    {
        var unqualified = new List<UnqualifiedAssigneeExceptionDto>();
        var overtime = new List<OvertimeViolationExceptionDto>();
        var weeklyCache = new Dictionary<(int, DateTimeOffset), List<ShiftEntity>>();

        foreach (var shift in shifts.Where(s => s.EmployeeId.HasValue))
        {
            if (!employees.TryGetValue(shift.EmployeeId!.Value, out var emp))
            {
                continue;
            }

            CheckQualification(shift, emp, unqualified);
            await CheckOvertimeAsync(shift, emp, weeklyCache, overtime, cancellationToken);
        }

        return (unqualified, overtime);
    }

    private static void CheckQualification(
        ShiftEntity shift,
        Employee emp,
        List<UnqualifiedAssigneeExceptionDto> list)
    {
        if (emp.JobRoleId == shift.JobRoleId)
        {
            return;
        }

        list.Add(BuildUnqualifiedDto(shift, emp));
    }

    private static string ResolveEmployeeName(Employee emp)
    {
        return $"{emp.FirstName} {emp.LastName}".Trim();
    }

    private static string ResolveRoleTitle(JobRole? role)
    {
        return role != null ? role.Title : string.Empty;
    }

    private static string ResolveShiftOrEmployeeRoleTitle(JobRole? shiftRole, JobRole? empRole)
    {
        if (shiftRole != null && !string.IsNullOrEmpty(shiftRole.Title))
        {
            return shiftRole.Title;
        }

        return empRole != null ? empRole.Title : string.Empty;
    }

    private static UnqualifiedAssigneeExceptionDto BuildUnqualifiedDto(ShiftEntity shift, Employee emp)
    {
        var date = GetShiftDate(shift);
        var start = shift.StartTime.TimeOfDay;
        var end = shift.EndTime.TimeOfDay;

        return new UnqualifiedAssigneeExceptionDto(
            shift.Id,
            emp.Id,
            ResolveEmployeeName(emp),
            shift.JobRoleId,
            ResolveRoleTitle(shift.JobRole),
            emp.JobRoleId,
            ResolveRoleTitle(emp.JobRole),
            date,
            start,
            end,
            FormatTimeRange(start, end)
        );
    }

    private static decimal ResolveOvertimeThreshold(PayrollProfile? profile)
    {
        if (profile != null && profile.OvertimeThresholdHours > 0)
        {
            return profile.OvertimeThresholdHours;
        }

        return 40.0m;
    }

    private static bool IsOvertimeViolation(decimal shiftHours, decimal totalWeekly, decimal threshold)
    {
        return shiftHours > 8.0m || totalWeekly > threshold;
    }

    private async Task CheckOvertimeAsync(
        ShiftEntity shift,
        Employee emp,
        Dictionary<(int, DateTimeOffset), List<ShiftEntity>> cache,
        List<OvertimeViolationExceptionDto> list,
        CancellationToken cancellationToken)
    {
        var (weekStart, weekEnd) = GetWeekBoundary(GetShiftDate(shift));
        var weeklyShifts = await GetEmployeeWeeklyShiftsAsync(emp.Id, weekStart, weekEnd, cache, cancellationToken);
        var shiftHours = (decimal)(shift.EndTime - shift.StartTime).TotalHours;
        var totalWeekly = CalculateTotalWeeklyHours(weeklyShifts, shift, shiftHours);
        var threshold = ResolveOvertimeThreshold(emp.PayrollProfile);

        if (IsOvertimeViolation(shiftHours, totalWeekly, threshold))
        {
            list.Add(BuildOvertimeDto(shift, emp, shiftHours, totalWeekly, threshold));
        }
    }

    private static decimal CalculateTotalWeeklyHours(
        IReadOnlyList<ShiftEntity> weeklyShifts,
        ShiftEntity currentShift,
        decimal currentShiftHours)
    {
        var total = weeklyShifts.Sum(s => (decimal)(s.EndTime - s.StartTime).TotalHours);
        if (!weeklyShifts.Any(s => s.Id == currentShift.Id))
        {
            total += currentShiftHours;
        }

        return total;
    }

    private async Task<List<ShiftEntity>> GetEmployeeWeeklyShiftsAsync(
        int employeeId,
        DateTimeOffset weekStart,
        DateTimeOffset weekEnd,
        Dictionary<(int, DateTimeOffset), List<ShiftEntity>> cache,
        CancellationToken cancellationToken)
    {
        var key = (employeeId, weekStart);
        if (cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var shifts = await _shiftRepository.Query(true)
            .Where(s => s.EmployeeId == employeeId && s.StartTime >= weekStart && s.StartTime < weekEnd)
            .ToListAsync(cancellationToken);

        cache[key] = shifts;
        return shifts;
    }

    private static OvertimeViolationExceptionDto BuildOvertimeDto(
        ShiftEntity shift,
        Employee emp,
        decimal shiftHours,
        decimal totalWeekly,
        decimal threshold)
    {
        var (projectedOt, reason) = CalculateOtHoursAndReason(shiftHours, totalWeekly, threshold);
        var date = GetShiftDate(shift);
        var start = shift.StartTime.TimeOfDay;
        var end = shift.EndTime.TimeOfDay;

        return new OvertimeViolationExceptionDto(
            shift.Id,
            emp.Id,
            ResolveEmployeeName(emp),
            shift.JobRoleId,
            ResolveShiftOrEmployeeRoleTitle(shift.JobRole, emp.JobRole),
            date,
            start,
            end,
            FormatTimeRange(start, end),
            shiftHours,
            totalWeekly,
            projectedOt,
            reason
        );
    }

    private static (decimal ProjectedOtHours, string Reason) CalculateOtHoursAndReason(
        decimal shiftHours,
        decimal totalWeekly,
        decimal threshold)
    {
        var isDailyOt = shiftHours > 8.0m;
        var isWeeklyOt = totalWeekly > threshold;

        if (isDailyOt && isWeeklyOt)
        {
            var dailyOt = shiftHours - 8.0m;
            var weeklyOt = totalWeekly - threshold;
            return (Math.Max(dailyOt, weeklyOt),
                $"Daily shift exceeds 8.0h ({shiftHours:0.#}h) and weekly hours exceed {threshold:0.#}h threshold ({totalWeekly:0.#}h).");
        }

        if (isDailyOt)
        {
            return (shiftHours - 8.0m,
                $"Daily shift duration of {shiftHours:0.#}h exceeds 8.0h limit.");
        }

        return (totalWeekly - threshold,
            $"Total weekly hours of {totalWeekly:0.#}h exceed {threshold:0.#}h threshold.");
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

    private static PreflightPublishScheduleResponseDto BuildResponse(
        int siteId,
        int totalShifts,
        List<DateOnly> scannedDates,
        List<UnqualifiedAssigneeExceptionDto> unqualified,
        List<OvertimeViolationExceptionDto> overtime)
    {
        var hasExceptions = unqualified.Count > 0 || overtime.Count > 0;

        return new PreflightPublishScheduleResponseDto(
            siteId,
            totalShifts,
            scannedDates.Count,
            scannedDates,
            unqualified,
            overtime,
            unqualified.Count,
            overtime.Count,
            hasExceptions,
            !hasExceptions
        );
    }

    private static PreflightPublishScheduleResponseDto BuildEmptyResponse(
        int siteId,
        List<DateOnly> scannedDates)
    {
        return new PreflightPublishScheduleResponseDto(
            siteId,
            0,
            scannedDates.Count,
            scannedDates,
            new List<UnqualifiedAssigneeExceptionDto>(),
            new List<OvertimeViolationExceptionDto>(),
            0,
            0,
            false,
            true
        );
    }
}
