namespace Buy2.Domain.Enums;

public enum AttendanceDayStatus
{
    Present = 1,
    ApprovedLeave = 2,
    UnapprovedLeave = 3,
    PartialLeave = 4,
    NoAttendance = 5,
    PublicHoliday = 6,
    AttendanceNotRequired = 7,
    OnTime = 8,
    Late = 9
}
