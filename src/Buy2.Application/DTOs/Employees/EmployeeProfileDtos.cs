using Buy2.Domain.Enums;

namespace Buy2.Application.DTOs.Employees;

// 1. Employee List Row DTO
public record EmployeeListRowDto(
    int Id,
    string EmployeeCode,
    string EmployeeName,
    DateTime JoinDate,
    string JobTitle,
    string Email,
    bool AdminAccess
);

// 2. Paginated List Response DTO
public record PaginatedListDto<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record PaginatedEmployeeListDto(
    List<EmployeeListRowDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

// 3. Employee Stats DTO (Profile Header)
public record EmployeeStatsDto(
    int TotalPoints,
    int TotalTasks,
    int TotalGifts
);

// 4. Personal Info DTO
public record EmployeePersonalInfoDto(
    DateTime? Birthdate,
    Gender Gender
);

// 5. Job Details DTO
public record EmployeeJobDetailsDto(
    string Title,
    string Department,
    string SeniorityLevel,
    int ExperienceYears,
    string? DirectManagerName,
    string JobType,
    List<string> Qualifications
);

// 6. Full Employee Profile DTO (Information Tab)
public record EmployeeProfileDto(
    int Id,
    string EmployeeCode,
    string FullName,
    string Phone,
    string Email,
    string Location,
    string? ProfilePhotoUrl,
    EmployeeStatsDto Stats,
    EmployeePersonalInfoDto PersonalInfo,
    EmployeeJobDetailsDto JobDetails
);

// 7. Update Partial Input DTOs
public record UpdatePersonalInfoRequestDto(
    DateTime? Birthdate,
    Gender? Gender
);

public record UpdateJobDetailsRequestDto(
    int? JobRoleId,
    string? SeniorityLevel,
    int? ExperienceYears,
    int? DirectManagerId,
    string? JobType
);

