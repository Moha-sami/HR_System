namespace Buy2.Domain.Enums;

public enum AttendanceDayStatus
{
    OnTime = 1,
    Late = 2,
    ApprovedLeave = 3,
    UnapprovedLeave = 4,
    PartialLeave = 5,
    NoAttendance = 6,
    PublicHoliday = 7,
    AttendanceNotRequired = 8
}
