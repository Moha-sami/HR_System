using System;
using System.Collections.Generic;

namespace Buy2.Application.Features.Jobs.DTOs;

public record JobListItemDto(
    int Id,
    string Title,
    int? DepartmentId,
    string? DepartmentName,
    string SeniorityLevel,
    int ExperienceYears,
    string AttendanceType,
    int AllocatedEmployeesCount,
    bool IsActive,
    DateTimeOffset CreatedAt
);

public record JobDetailsDto(
    int Id,
    string Title,
    string? Description,
    int? DepartmentId,
    string? DepartmentName,
    string SeniorityLevel,
    int ExperienceYears,
    string AttendanceType,
    List<string> OnlineWorkdays,
    List<string> OfflineWorkdays,
    List<string> RequiredQualifications,
    int AllocatedEmployeesCount,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
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

public record JobFilterQueryDto(
    string? SearchTerm = null,
    int? DepartmentId = null,
    string? AttendanceType = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 10
);

public record JobPaginatedResponseDto<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);
