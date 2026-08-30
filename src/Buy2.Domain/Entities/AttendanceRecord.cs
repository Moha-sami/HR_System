using Buy2.Domain.Enums;

namespace Buy2.Domain.Entities;

public class AttendanceRecord : BaseEntity
{
    public int EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public DateTimeOffset? ClockInTime { get; set; }
    public DateTimeOffset? ClockOutTime { get; set; }
    public int? ScheduledShiftId { get; set; }
    public int? LatenessMinutes { get; set; }
    public int? EarlyLeaveMinutes { get; set; }
    public decimal ScheduledHours { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal BreakMinutes { get; set; }
    public AttendanceDayStatus Status { get; set; }
    public string? LeaveType { get; set; }
    public string? Notes { get; set; }

    // Navigation Property
    public Employee? Employee { get; set; }
    public ShiftEntity? ScheduledShift { get; set; }
}
