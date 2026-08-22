using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Employees;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.GetAttendanceCalendar;

public record GetAttendanceCalendarQuery(
    int EmployeeId,
    int? Month = null,
    int? Year = null
) : IRequest<AttendanceCalendarDto?>;

public class GetAttendanceCalendarQueryHandler : IRequestHandler<GetAttendanceCalendarQuery, AttendanceCalendarDto?>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<AttendanceRecord> _attendanceRepository;

    public GetAttendanceCalendarQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<AttendanceRecord> attendanceRepository)
    {
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
    }

    public async Task<AttendanceCalendarDto?> Handle(GetAttendanceCalendarQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate employee existence and active status
        var employee = await _employeeRepository.Query()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null || employee.IsDeleted)
        {
            return null;
        }

        // 2. Resolve Year and Month (defaulting to current UTC if null/out of bounds)
        var now = DateTime.UtcNow;
        var year = request.Year ?? now.Year;
        if (year < 1900 || year > 2100)
        {
            year = now.Year;
        }

        var month = request.Month ?? now.Month;
        if (month < 1 || month > 12)
        {
            month = Math.Clamp(month, 1, 12);
        }

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = new DateTime(year, month, daysInMonth, 23, 59, 59, 999, DateTimeKind.Utc);

        // 3. Query all attendance records for the month
        var records = await _attendanceRepository.Query()
            .Where(a => a.EmployeeId == request.EmployeeId && a.Date >= monthStart.Date && a.Date <= monthEnd.Date)
            .ToListAsync(cancellationToken);

        var recordByDay = records
            .GroupBy(r => r.Date.Date)
            .ToDictionary(g => g.Key, g => g.First());

        // 4. Construct daily calendar items
        const decimal targetHoursPerDay = 8.0m;
        var days = new List<AttendanceDayDto>(daysInMonth);

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
            var isWeekend = date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday;

            if (recordByDay.TryGetValue(date.Date, out var record))
            {
                var hoursWorked = record.HoursWorked;
                if (hoursWorked <= 0 && record.ClockInTime.HasValue && record.ClockOutTime.HasValue)
                {
                    var calculatedHours = (decimal)(record.ClockOutTime.Value - record.ClockInTime.Value).TotalHours;
                    hoursWorked = Math.Max(0m, Math.Round(calculatedHours, 2));
                }

                var status = record.Status;
                if ((int)status == 0)
                {
                    status = (record.LatenessMinutes.HasValue && record.LatenessMinutes.Value > 0)
                        ? AttendanceDayStatus.Late
                        : AttendanceDayStatus.OnTime;
                }

                var hoursLeft = Math.Max(0m, targetHoursPerDay - hoursWorked);

                days.Add(new AttendanceDayDto(
                    Date: date,
                    Status: status,
                    LeaveType: record.LeaveType,
                    HoursWorked: hoursWorked,
                    HoursLeft: hoursLeft,
                    OtHours: record.OvertimeHours,
                    BreakTime: record.BreakMinutes,
                    LatenessMinutes: record.LatenessMinutes
                ));
            }
            else
            {
                var status = isWeekend ? AttendanceDayStatus.AttendanceNotRequired : AttendanceDayStatus.NoAttendance;
                var hoursLeft = isWeekend ? 0m : targetHoursPerDay;

                days.Add(new AttendanceDayDto(
                    Date: date,
                    Status: status,
                    LeaveType: null,
                    HoursWorked: 0m,
                    HoursLeft: hoursLeft,
                    OtHours: 0m,
                    BreakTime: 0m,
                    LatenessMinutes: null
                ));
            }
        }

        // 5. Compute attendance summary
        var recordedHours = Math.Round(days.Sum(d => d.HoursWorked), 2);
        var totalWorkdaysCount = days.Count(d => d.Status != AttendanceDayStatus.AttendanceNotRequired && d.Date.DayOfWeek != DayOfWeek.Friday && d.Date.DayOfWeek != DayOfWeek.Saturday);
        var targetHours = Math.Round(totalWorkdaysCount * targetHoursPerDay, 2);

        var attendedDaysCount = days.Count(d => d.Status == AttendanceDayStatus.OnTime 
            || d.Status == AttendanceDayStatus.Late 
            || d.Status == AttendanceDayStatus.PartialLeave 
            || d.HoursWorked > 0);

        var onTimeDaysCount = days.Count(d => d.Status == AttendanceDayStatus.OnTime);

        var lateDays = days.Where(d => d.Status == AttendanceDayStatus.Late || (d.LatenessMinutes.HasValue && d.LatenessMinutes.Value > 0)).ToList();

        var attendanceRate = totalWorkdaysCount > 0
            ? Math.Round(((decimal)attendedDaysCount / totalWorkdaysCount) * 100m, 2)
            : 0m;

        var punctualityScore = attendedDaysCount > 0
            ? Math.Round(((decimal)onTimeDaysCount / attendedDaysCount) * 100m, 2)
            : 100m;

        var averageLatenessMinutes = lateDays.Count > 0
            ? Math.Round((decimal)lateDays.Average(d => d.LatenessMinutes ?? 0), 2)
            : 0m;

        var summary = new AttendanceSummaryDto(
            AttendanceRate: attendanceRate,
            PunctualityScore: punctualityScore,
            AverageLatenessMinutes: averageLatenessMinutes,
            RecordedHours: recordedHours,
            TargetHours: targetHours
        );

        return new AttendanceCalendarDto(summary, days);
    }
}
