using Buy2.Application.DTOs.Schedules;
using Buy2.Domain.Entities;

namespace Buy2.Application.Features.Schedules.ApplyTemplate.Services;

/// <summary>
/// Pure analytics: labor-cost pricing + coverage status.
/// No repository access — fully unit-testable.
/// </summary>
public interface IScheduleAnalyticsService
{
    (decimal Total, decimal Regular, decimal Overtime) CalculateLaborCost(
        IReadOnlyList<ShiftEntity> dayShifts,
        IReadOnlyDictionary<int, Employee> employees,
        IReadOnlyDictionary<int, decimal> weekHoursBefore);

    WeekDayCalendarStatus DetermineCoverageStatus(
        bool isDayOff,
        IReadOnlyList<ShiftEntity> dayShifts,
        IReadOnlyDictionary<int, Employee> employees);
}
