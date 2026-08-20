using Buy2.Domain.Enums;

namespace Buy2.Application.Features.Employees.BulkOnboard;

public class BulkOnboardEmployeeItemDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? EmployeeCode { get; set; }
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
    public int? JobRoleId { get; set; }
    public string? JobTitle { get; set; }
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }
    public int? DirectManagerId { get; set; }
    public string? SeniorityLevel { get; set; }
    public int? ExperienceYears { get; set; }
    public string? JobType { get; set; }
    public string? AttendanceType { get; set; }
    public Gender? Gender { get; set; }
    public DateTime? Birthdate { get; set; }
    public string? DefaultPassword { get; set; }
}

public record BulkOnboardRowErrorDto(
    int RowIndex,
    string? Email,
    string ErrorMessage
);

public record BulkOnboardResultDto(
    int TotalCount,
    int CreatedCount,
    int FailedCount,
    List<BulkOnboardRowErrorDto> FailedRows
);
