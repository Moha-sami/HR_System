using Buy2.Domain.Enums;

namespace Buy2.Application.DTOs.Employees;

// Attendance Summary DTO
public record AttendanceSummaryDto(
    decimal AttendanceRate,
    decimal PunctualityScore,
    decimal AverageLatenessMinutes,
    decimal RecordedHours,
    decimal TargetHours
);

// Attendance Day DTO for Calendar Cell
public record AttendanceDayDto(
    DateTime Date,
    AttendanceDayStatus Status,
    string? LeaveType,
    decimal HoursWorked,
    decimal HoursLeft,
    decimal OtHours,
    decimal BreakTime,
    int? LatenessMinutes
);

// Attendance Calendar DTO
public record AttendanceCalendarDto(
    AttendanceSummaryDto Summary,
    List<AttendanceDayDto> Days
);
