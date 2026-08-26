namespace Buy2.Application.Features.Employees.UpdateJobDetails;

public record UpdateJobDetailsDto(
    int? JobRoleId = null,
    int? DirectManagerId = null,
    string? SeniorityLevel = null,
    int? ExperienceYears = null,
    string? JobType = null,
    string? AttendanceType = null,
    List<string>? OnlineWorkdays = null,
    List<string>? OfflineWorkdays = null,
    List<string>? Qualifications = null
);
