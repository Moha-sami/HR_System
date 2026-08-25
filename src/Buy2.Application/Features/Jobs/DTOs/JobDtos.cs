using System;
using System.Collections.Generic;

namespace Buy2.Application.Features.Jobs.DTOs;

public record JobListItemDto(
    int Id,
    string Title,
    int? DepartmentId,
    string? DepartmentName,
    string SeniorityLevel,
    string WorkModel,
    int AssignedEmployeesCount,
    int RequiredQualificationsCount,
    int ExperienceYearsMin,
    bool IsActive,
    DateTimeOffset CreatedAt
);

public record JobDetailsDto(
    int Id,
    string Title,
    int? DepartmentId,
    string? DepartmentName,
    string SeniorityLevel,
    string? Description,
    List<string> RequiredQualifications,
    int ExperienceYearsMin,
    string WorkModel,
    List<string> OnlineWorkdays,
    List<string> OfflineWorkdays,
    int AssignedEmployeesCount,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public record CreateJobDto(
    string Title,
    int? DepartmentId,
    string? NewDepartmentName,
    string SeniorityLevel,
    string? Description,
    List<string>? RequiredQualifications,
    int ExperienceYearsMin,
    string WorkModel,
    List<string>? OnlineWorkdays,
    List<string>? OfflineWorkdays
);

public record UpdateJobDto(
    string Title,
    int? DepartmentId,
    string SeniorityLevel,
    string? Description,
    List<string>? RequiredQualifications,
    int ExperienceYearsMin,
    string WorkModel,
    List<string>? OnlineWorkdays,
    List<string>? OfflineWorkdays,
    bool IsActive
);

public record JobFilterQueryDto(
    string? SearchTerm = null,
    int? DepartmentId = null,
    string? SeniorityLevel = null,
    string? WorkModel = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 10,
    string? SortBy = null,
    string? SortDir = null
);

public record JobPaginatedResponseDto<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);

public record AffectedEmployeeDto(
    int Id,
    string EmployeeCode,
    string FullName,
    string Email,
    string SiteName,
    string? ProfilePhotoUrl
);

public record JobDeletionImpactDto(
    int JobId,
    string JobTitle,
    int AssignedEmployeesCount,
    bool CanDeleteDirectly,
    List<AffectedEmployeeDto> AffectedEmployees
);

public record ReassignEmployeesAndDeleteJobDto(
    int ReplacementJobId
);

public record JobAssignedEmployeeListItemDto(
    int Id,
    string EmployeeCode,
    string FullName,
    string Email,
    string DepartmentName,
    string SiteName,
    DateTime? JoinDate,
    string? ProfilePhotoUrl
);

public record JobEmployeeRosterItemDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string Email,
    string Phone,
    string SiteName,
    string DepartmentName,
    DateTime? HiredDate,
    string Status
);
