namespace Buy2.Application.DTOs.Schedules;

public enum AssignmentConflictType
{
    None = 0,
    QualificationMismatch = 1,
    OvertimeRisk = 2,
    MultipleConflicts = 3
}

public record AssignmentConflictWarningDto(
    AssignmentConflictType ConflictType,
    string Message,
    string? Details = null
);

public record AssignEmployeeToShiftRequestDto(
    int EmployeeId,
    bool ConfirmOverride = false,
    string? OverrideReason = null
);

public record AssignEmployeeToShiftResponseDto(
    bool Success,
    bool HasConflicts,
    List<AssignmentConflictWarningDto> Warnings,
    bool WasOverridden,
    string? OverrideReason,
    DailyShiftBlockDto? UpdatedBlock,
    decimal UpdatedDailyLaborCost
);
