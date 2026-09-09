namespace Buy2.Application.DTOs.Schedules;

public enum ShiftMarketDispatchPolicy
{
    None = 0,
    FirstComeFirstServe = 1,
    ClaimRequest = 2
}

public record CreateShiftBlockRequestDto(
    int SiteId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int JobRoleId,
    ShiftMarketDispatchPolicy DispatchPolicy = ShiftMarketDispatchPolicy.None
);

public record ShiftBlockResponseDto(
    int ShiftId,
    int SiteId,
    int JobRoleId,
    string RoleTitle,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsPublished,
    int? EmployeeId,
    string Status,
    ShiftMarketDispatchPolicy DispatchPolicy,
    DateTimeOffset CreatedAt
);
