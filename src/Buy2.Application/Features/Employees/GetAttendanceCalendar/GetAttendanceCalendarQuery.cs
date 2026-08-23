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
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IRepository<Request> _requestRepository;

    public GetAttendanceCalendarQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<AttendanceRecord> attendanceRepository,
        IRepository<ShiftEntity> shiftRepository,
        IRepository<Request> requestRepository)
    {
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _shiftRepository = shiftRepository;
        _requestRepository = requestRepository;
    }

    public async Task<AttendanceCalendarDto?> Handle(GetAttendanceCalendarQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate employee existence and active status
        var employee = await _employeeRepository.Query()
            .AsNoTracking()
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

        // 3. Query all attendance records for the month eagerly including ScheduledShift
        var records = await _attendanceRepository.Query()
            .AsNoTracking()
            .Include(a => a.ScheduledShift)
            .Where(a => a.EmployeeId == request.EmployeeId && a.Date >= monthStart.Date && a.Date <= monthEnd.Date)
            .ToListAsync(cancellationToken);

        var recordByDay = records
            .GroupBy(r => r.Date.Date)
            .ToDictionary(g => g.Key, g => g.First());

        // 4. Query Scheduled Shifts for the employee during the month
        var monthStartOffset = new DateTimeOffset(monthStart);
        var monthEndOffset = new DateTimeOffset(monthEnd);

        var scheduledShifts = await _shiftRepository.Query()
            .AsNoTracking()
            .Where(s => s.EmployeeId == request.EmployeeId &&
                        s.StartTime <= monthEndOffset &&
                        s.EndTime >= monthStartOffset)
            .ToListAsync(cancellationToken);

        var shiftById = scheduledShifts.ToDictionary(s => s.Id);
        var shiftsByDate = scheduledShifts
            .GroupBy(s => s.StartTime.UtcDateTime.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 5. Query Approved Leave Requests for the employee during the month
        var leaveRequests = await _requestRepository.Query()
            .AsNoTracking()
            .Include(r => r.RequestType)
            .Where(r => r.EmployeeId == request.EmployeeId &&
                        r.StartDate.HasValue &&
                        r.StartDate.Value <= monthEnd &&
                        (!r.EndDate.HasValue || r.EndDate.Value >= monthStart))
            .ToListAsync(cancellationToken);

        var approvedLeaveRequests = leaveRequests
            .Where(IsApprovedLeaveRequest)
            .ToList();

        // 6. Construct daily calendar items
        const decimal defaultTargetHoursPerDay = 8.0m;
        var days = new List<AttendanceDayDto>(daysInMonth);

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
            var isWeekend = date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday;

            // Resolve scheduled shift for the date
            ShiftEntity? shiftForDay = null;
            if (recordByDay.TryGetValue(date.Date, out var record) && record.ScheduledShift != null)
            {
                shiftForDay = record.ScheduledShift;
            }
            else if (record?.ScheduledShiftId != null && shiftById.TryGetValue(record.ScheduledShiftId.Value, out var matchedShift))
            {
                shiftForDay = matchedShift;
            }
            else if (shiftsByDate.TryGetValue(date.Date, out var dayShifts))
            {
                shiftForDay = dayShifts.FirstOrDefault();
            }

            var (scheduledHours, targetBreakMinutes) = CalculateShiftMetrics(shiftForDay, defaultTargetHoursPerDay);

            // Resolve approved leave for the date
            var leaveForDay = approvedLeaveRequests.FirstOrDefault(r =>
                r.StartDate.HasValue &&
                date.Date >= r.StartDate.Value.Date &&
                (!r.EndDate.HasValue || date.Date <= r.EndDate.Value.Date));

            if (record != null)
            {
                var hoursWorked = record.HoursWorked;
                if (hoursWorked <= 0 && record.ClockInTime.HasValue && record.ClockOutTime.HasValue)
                {
                    var calculatedHours = (decimal)(record.ClockOutTime.Value - record.ClockInTime.Value).TotalHours;
                    hoursWorked = Math.Max(0m, Math.Round(calculatedHours, 2));
                }

                var breakTime = record.BreakMinutes > 0 ? record.BreakMinutes : targetBreakMinutes;

                var leaveType = !string.IsNullOrWhiteSpace(record.LeaveType)
                    ? record.LeaveType
                    : FormatLeaveType(leaveForDay);

                var status = record.Status;
                if (leaveForDay != null && (status == AttendanceDayStatus.NoAttendance || (int)status == 0 || status == AttendanceDayStatus.ApprovedLeave))
                {
                    status = AttendanceDayStatus.ApprovedLeave;
                }
                else if ((int)status == 0)
                {
                    status = (record.LatenessMinutes.HasValue && record.LatenessMinutes.Value > 0)
                        ? AttendanceDayStatus.Late
                        : AttendanceDayStatus.OnTime;
                }

                var hoursLeft = leaveForDay != null ? 0m : Math.Max(0m, scheduledHours - hoursWorked);

                days.Add(new AttendanceDayDto(
                    Date: date,
                    Status: status,
                    LeaveType: leaveType,
                    HoursWorked: hoursWorked,
                    HoursLeft: hoursLeft,
                    OtHours: record.OvertimeHours,
                    BreakTime: breakTime,
                    LatenessMinutes: record.LatenessMinutes
                ));
            }
            else
            {
                if (leaveForDay != null)
                {
                    var leaveType = FormatLeaveType(leaveForDay);
                    days.Add(new AttendanceDayDto(
                        Date: date,
                        Status: AttendanceDayStatus.ApprovedLeave,
                        LeaveType: leaveType,
                        HoursWorked: 0m,
                        HoursLeft: 0m,
                        OtHours: 0m,
                        BreakTime: targetBreakMinutes,
                        LatenessMinutes: null
                    ));
                }
                else
                {
                    var status = isWeekend ? AttendanceDayStatus.AttendanceNotRequired : AttendanceDayStatus.NoAttendance;
                    var hoursLeft = isWeekend ? 0m : scheduledHours;
                    var breakTime = isWeekend ? 0m : targetBreakMinutes;

                    days.Add(new AttendanceDayDto(
                        Date: date,
                        Status: status,
                        LeaveType: null,
                        HoursWorked: 0m,
                        HoursLeft: hoursLeft,
                        OtHours: 0m,
                        BreakTime: breakTime,
                        LatenessMinutes: null
                    ));
                }
            }
        }

        // 7. Compute attendance summary
        var recordedHours = Math.Round(days.Sum(d => d.HoursWorked), 2);
        var workdays = days.Where(d => d.Status != AttendanceDayStatus.AttendanceNotRequired &&
                                       d.Date.DayOfWeek != DayOfWeek.Friday &&
                                       d.Date.DayOfWeek != DayOfWeek.Saturday &&
                                       d.Status != AttendanceDayStatus.ApprovedLeave).ToList();

        var totalWorkdaysCount = workdays.Count;
        var targetHours = Math.Round(workdays.Sum(d => d.HoursWorked + d.HoursLeft), 2);

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

    private static bool IsApprovedLeaveRequest(Request req)
    {
        if (string.IsNullOrWhiteSpace(req.Status))
        {
            return false;
        }

        var statusLower = req.Status.Trim().ToLowerInvariant();
        return statusLower == "approved" ||
               statusLower == "approved leave" ||
               statusLower == "approvedleave" ||
               statusLower == "sick leave" ||
               statusLower == "remote work";
    }

    private static (decimal ScheduledHours, decimal TargetBreakMinutes) CalculateShiftMetrics(ShiftEntity? shift, decimal defaultHours)
    {
        if (shift == null)
        {
            return (defaultHours, 60m);
        }

        var totalHours = (decimal)(shift.EndTime - shift.StartTime).TotalHours;
        if (totalHours <= 0)
        {
            return (defaultHours, 60m);
        }

        var scheduledHours = Math.Round(totalHours, 2);
        var targetBreakMinutes = scheduledHours >= 8.0m ? 60m : (scheduledHours >= 4.0m ? 30m : 0m);

        return (scheduledHours, targetBreakMinutes);
    }

    private static string FormatLeaveType(Request? request)
    {
        if (request == null)
        {
            return "Approved Leave";
        }

        if (request.RequestType != null && !string.IsNullOrWhiteSpace(request.RequestType.Name))
        {
            return request.RequestType.Name;
        }

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            return request.Reason;
        }

        return "Approved Leave";
    }
}
