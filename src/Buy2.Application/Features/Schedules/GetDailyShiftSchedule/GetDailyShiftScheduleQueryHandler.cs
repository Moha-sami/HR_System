using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Schedules;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Schedules.GetDailyShiftSchedule;

public class GetDailyShiftScheduleQueryHandler : IRequestHandler<GetDailyShiftScheduleQuery, DailyShiftScheduleResponseDto>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<SiteOperationalHour>? _operationalHourRepository;

    public GetDailyShiftScheduleQueryHandler(
        IRepository<Site> siteRepository,
        IRepository<ShiftEntity> shiftRepository,
        IRepository<Employee> employeeRepository)
        : this(siteRepository, shiftRepository, employeeRepository, null)
    {
    }

    public GetDailyShiftScheduleQueryHandler(
        IRepository<Site> siteRepository,
        IRepository<ShiftEntity> shiftRepository,
        IRepository<Employee> employeeRepository,
        IRepository<SiteOperationalHour>? operationalHourRepository)
    {
        _siteRepository = siteRepository;
        _shiftRepository = shiftRepository;
        _employeeRepository = employeeRepository;
        _operationalHourRepository = operationalHourRepository;
    }

    public async Task<DailyShiftScheduleResponseDto> Handle(GetDailyShiftScheduleQuery request, CancellationToken cancellationToken)
    {
        var site = await _siteRepository.Query(true)
            .Include(s => s.OperationalHours)
            .FirstOrDefaultAsync(s => s.Id == request.SiteId, cancellationToken);

        if (site == null)
        {
            throw new KeyNotFoundException($"Site with ID {request.SiteId} not found.");
        }

        var operationalHours = await ResolveOperationalHoursAsync(site, cancellationToken);

        var (weekStart, weekEnd) = GetWeekBoundary(request.Date);

        var shifts = await _shiftRepository.Query(true)
            .Include(s => s.JobRole)
            .Where(s => s.SiteId == request.SiteId && s.StartTime >= weekStart.AddDays(-1) && s.StartTime < weekEnd.AddDays(1))
            .ToListAsync(cancellationToken);

        var employees = await LoadEmployeesAsync(shifts, cancellationToken);

        var targetDateShifts = shifts.Where(s => IsShiftOnDate(s, request.Date)).OrderBy(s => s.StartTime).ToList();

        var isDayOff = IsSiteDayOff(request.Date.DayOfWeek, operationalHours);
        var totalLaborCost = CalculateLaborCost(targetDateShifts, employees);
        var weekCalendarStrip = BuildWeekCalendarStrip(request.Date, shifts, employees, operationalHours);
        var hourlyTimeline = BuildHourlyTimeline(request.Date, targetDateShifts, employees);

        return new DailyShiftScheduleResponseDto(
            SiteId: site.Id,
            SiteName: site.SiteName,
            Date: request.Date,
            IsDayOff: isDayOff,
            TotalEstimatedLaborCost: totalLaborCost,
            WeekCalendarStrip: weekCalendarStrip,
            HourlyTimeline: hourlyTimeline
        );
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

    private async Task<Dictionary<DayOfWeek, SiteOperationalHour>> ResolveOperationalHoursAsync(
        Site site,
        CancellationToken cancellationToken)
    {
        if (_operationalHourRepository != null)
        {
            var fromRepo = await _operationalHourRepository.Query(true)
                .Where(o => o.SiteId == site.Id)
                .ToListAsync(cancellationToken);

            if (fromRepo.Count > 0)
            {
                return fromRepo.GroupBy(o => o.DayOfWeek).ToDictionary(g => g.Key, g => g.First());
            }
        }

        var source = site.OperationalHours ?? Enumerable.Empty<SiteOperationalHour>();
        return source.GroupBy(o => o.DayOfWeek).ToDictionary(g => g.Key, g => g.First());
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
            .Include(e => e.PayrollProfile)
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);
    }

    private static bool IsShiftOnDate(ShiftEntity shift, DateOnly date)
    {
        return DateOnly.FromDateTime(shift.StartTime.Date) == date ||
               DateOnly.FromDateTime(shift.StartTime.UtcDateTime.Date) == date;
    }

    private static bool IsSiteDayOff(
        DayOfWeek dayOfWeek,
        IReadOnlyDictionary<DayOfWeek, SiteOperationalHour> operationalHours)
    {
        if (operationalHours.TryGetValue(dayOfWeek, out var hourRecord))
        {
            return !hourRecord.IsOpen;
        }

        return false;
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

    private static decimal CalculateLaborCost(
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

    private static string DetermineBlockColor(ShiftEntity shift, Employee? employee)
    {
        if (shift.EmployeeId == null)
        {
            return "#FFA500"; // Orange / Open slot
        }

        if (!shift.IsPublished)
        {
            return "#3B82F6"; // Blue / Unpublished draft
        }

        if (employee != null && employee.JobRoleId != shift.JobRoleId)
        {
            return "#EF4444"; // Red / Misallocation
        }

        return "#10B981"; // Green / Covered
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

    private static WeekDayCalendarStatus DetermineDayStatus(
        bool isDayOff,
        IReadOnlyList<ShiftEntity> dayShifts,
        IReadOnlyDictionary<int, Employee> employees)
    {
        if (isDayOff)
        {
            return WeekDayCalendarStatus.DimmedDayOff;
        }

        if (dayShifts.Count == 0)
        {
            return WeekDayCalendarStatus.NoAllocations;
        }

        if (HasOvertimeOrMisallocation(dayShifts, employees))
        {
            return WeekDayCalendarStatus.OvertimeOrMisallocation;
        }

        if (dayShifts.Any(s => s.EmployeeId == null || !s.IsPublished))
        {
            return WeekDayCalendarStatus.MissingResourcesOrUnpublished;
        }

        return WeekDayCalendarStatus.CoveredAndPublished;
    }

    private static List<CalendarDayBadgeDto> BuildWeekCalendarStrip(
        DateOnly targetDate,
        IReadOnlyList<ShiftEntity> shifts,
        IReadOnlyDictionary<int, Employee> employees,
        IReadOnlyDictionary<DayOfWeek, SiteOperationalHour> operationalHours)
    {
        int diff = (7 + (int)targetDate.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        var monday = targetDate.AddDays(-diff);

        var strip = new List<CalendarDayBadgeDto>(7);
        for (int i = 0; i < 7; i++)
        {
            var currentDay = monday.AddDays(i);
            var isDayOff = IsSiteDayOff(currentDay.DayOfWeek, operationalHours);
            var dayShifts = shifts.Where(s => IsShiftOnDate(s, currentDay)).ToList();
            var status = DetermineDayStatus(isDayOff, dayShifts, employees);

            strip.Add(new CalendarDayBadgeDto(
                Date: currentDay,
                DayOfWeek: currentDay.DayOfWeek,
                Status: status,
                IsSelected: currentDay == targetDate,
                IsDayOff: isDayOff
            ));
        }

        return strip;
    }

    private static List<TimelineHourlyIntervalDto> BuildHourlyTimeline(
        DateOnly targetDate,
        IReadOnlyList<ShiftEntity> targetDateShifts,
        IReadOnlyDictionary<int, Employee> employees)
    {
        int startHour = 9;
        int endHour = 17;

        if (targetDateShifts.Count > 0)
        {
            var minHour = targetDateShifts.Min(s => s.StartTime.Hour);
            if (minHour < startHour)
            {
                startHour = minHour;
            }

            var maxHour = targetDateShifts.Max(s => s.EndTime.Hour + (s.EndTime.Minute > 0 || s.EndTime.Second > 0 ? 1 : 0));
            if (maxHour > endHour)
            {
                endHour = Math.Min(24, maxHour);
            }
        }

        var timeline = new List<TimelineHourlyIntervalDto>();
        for (int h = startHour; h < endHour; h++)
        {
            var start = new TimeOnly(h, 0);
            var end = h == 23 ? new TimeOnly(23, 59, 59) : new TimeOnly(h + 1, 0);

            var blocks = targetDateShifts
                .Where(s => IsShiftInHourInterval(s, targetDate, h))
                .Select(s => CreateBlockDto(s, employees))
                .ToList();

            timeline.Add(new TimelineHourlyIntervalDto(start, end, blocks));
        }

        return timeline;
    }

    private static bool IsShiftInHourInterval(ShiftEntity shift, DateOnly targetDate, int hour)
    {
        var intervalStart = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.Zero);
        var intervalEnd = intervalStart.AddHours(1);

        return shift.StartTime < intervalEnd && shift.EndTime > intervalStart;
    }

    private static DailyShiftBlockDto CreateBlockDto(
        ShiftEntity shift,
        IReadOnlyDictionary<int, Employee> employees)
    {
        Employee? employee = null;
        if (shift.EmployeeId.HasValue)
        {
            employees.TryGetValue(shift.EmployeeId.Value, out employee);
        }

        var employeeName = employee != null ? $"{employee.FirstName} {employee.LastName}".Trim() : null;
        var colorCode = DetermineBlockColor(shift, employee);

        return new DailyShiftBlockDto(
            ShiftId: shift.Id,
            SiteId: shift.SiteId,
            JobRoleId: shift.JobRoleId,
            RoleTitle: shift.JobRole?.Title ?? string.Empty,
            StartTime: shift.StartTime,
            EndTime: shift.EndTime,
            IsPublished: shift.IsPublished,
            EmployeeId: shift.EmployeeId,
            EmployeeName: employeeName,
            EmployeeAvatarUrl: employee?.ProfilePhotoUrl,
            StatusColorCode: colorCode
        );
    }
}
