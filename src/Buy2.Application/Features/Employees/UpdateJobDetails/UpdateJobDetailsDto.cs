namespace Buy2.Application.Features.Employees.UpdateJobDetails;

public record UpdateJobDetailsDto(
    int? JobRoleId = null,
    int? RoleId = null,
    int? SiteId = null,
    int? DirectManagerId = null,
    string? SeniorityLevel = null,
    int? ExperienceYears = null,
    string? JobType = null,
    string? AttendanceType = null,
    DateTimeOffset? JoinDate = null
);
