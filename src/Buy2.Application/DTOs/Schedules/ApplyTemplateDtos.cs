namespace Buy2.Application.DTOs.Schedules;

public static class ApplyTemplateStripCodes
{
    public const string EmployeeNotFound = "EMPLOYEE_NOT_FOUND";
    public const string EmployeeInactive = "EMPLOYEE_INACTIVE";
    public const string SiteUnauthorized = "SITE_UNAUTHORIZED";
    public const string SiteClosed = "SITE_CLOSED";
    public const string OutsideOperationalHours = "OUTSIDE_OPERATIONAL_HOURS";
    public const string EmployeeOnLeave = "EMPLOYEE_ON_LEAVE";
    public const string EmployeeRemoteWork = "EMPLOYEE_REMOTE_WORK";
    public const string OutsideEmployeeAvailability = "OUTSIDE_EMPLOYEE_AVAILABILITY";
    public const string QualificationMismatch = "QUALIFICATION_MISMATCH";
}

public static class ApplyTemplateCollisionTypes
{
    public const string EmployeeOverlap = "EMPLOYEE_OVERLAP";
}

public static class ApplyTemplateConflictTypes
{
    public const string RoleSlotConflict = "ROLE_SLOT_CONFLICT";
}

public record ApplyTemplateRequestDto(
    int TemplateId,
    string? Keep = null
);

public record ApplyTemplateWarningDto(
    int? EmployeeId,
    string EmployeeName,
    string Role,
    string Reason,
    string Code
);

public record AppliedTemplateBlockDto(
    int ShiftId,
    int SiteId,
    int JobRoleId,
    string RoleTitle,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsPublished,
    int? EmployeeId,
    string? EmployeeName,
    bool IsOpenRole,
    bool Stripped,
    string? StripCode,
    string? StripReason,
    bool Collision,
    string? CollisionType,
    bool Conflict,
    string? ConflictType,
    bool Pruned
);

public record ApplyTemplateResponseDto(
    int SiteId,
    DateOnly Date,
    int TemplateId,
    List<AppliedTemplateBlockDto> Blocks,
    List<ApplyTemplateWarningDto> Warnings,
    decimal TotalLaborCost,
    decimal RegularCost,
    decimal OvertimeCost,
    WeekDayCalendarStatus CoverageStatus,
    int PrunedCount
);
