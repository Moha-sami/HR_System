namespace Buy2.Application.DTOs;

public record ShiftDto(
    int Id,
    int? EmployeeId,
    int SiteId,
    int JobRoleId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsPublished
);
