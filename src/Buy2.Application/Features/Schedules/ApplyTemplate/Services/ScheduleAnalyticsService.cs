using Buy2.Application.DTOs.Schedules;
using Buy2.Domain.Entities;

namespace Buy2.Application.Features.Schedules.ApplyTemplate.Services;

public sealed class ScheduleAnalyticsService : IScheduleAnalyticsService
{
    public (decimal Total, decimal Regular, decimal Overtime) CalculateLaborCost(
        IReadOnlyList<ShiftEntity> dayShifts,
        IReadOnlyDictionary<int, Employee> employees,
        IReadOnlyDictionary<int, decimal> weekHoursBefore)
    {
        decimal regular = 0m;
        decimal overtime = 0m;

        var assigned = dayShifts.Where(s => s.EmployeeId.HasValue).GroupBy(s => s.EmployeeId!.Value);
        foreach (var group in assigned)
        {
            if (!employees.TryGetValue(group.Key, out var emp))
            {
                continue;
            }

            PriceEmployeeDay(group, emp, weekHoursBefore, ref regular, ref overtime);
        }

        return (Math.Round(regular + overtime, 2), Math.Round(regular, 2), Math.Round(overtime, 2));
    }

    public WeekDayCalendarStatus DetermineCoverageStatus(
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

    private static void PriceEmployeeDay(
        IEnumerable<ShiftEntity> dayShifts,
        Employee employee,
        IReadOnlyDictionary<int, decimal> weekHoursBefore,
        ref decimal regular,
        ref decimal overtime)
    {
        var rate = CalculateHourlyRate(employee.PayrollProfile);
        var overtimeRate = employee.PayrollProfile?.OvertimeHourlyRate > 0
            ? employee.PayrollProfile.OvertimeHourlyRate
            : rate * 1.5m;
        var threshold = employee.PayrollProfile?.OvertimeThresholdHours > 0
            ? employee.PayrollProfile.OvertimeThresholdHours
            : 40m;
        var cumulative = weekHoursBefore.TryGetValue(employee.Id, out var before) ? before : 0m;

        foreach (var shift in dayShifts.OrderBy(s => s.StartTime))
        {
            var duration = (decimal)Math.Max(0, (shift.EndTime - shift.StartTime).TotalHours);
            var regularHours = Math.Max(0, Math.Min(duration, threshold - cumulative));
            regular += regularHours * rate;
            overtime += (duration - regularHours) * overtimeRate;
            cumulative += duration;
        }
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
}
