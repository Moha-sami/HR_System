using System;

namespace Buy2.Application.Features.Departments.DTOs;

public record DepartmentDto(
    int Id,
    string Name,
    string? Code,
    string? Description,
    int? HeadEmployeeId,
    string? HeadEmployeeName,
    int JobRolesCount,
    int EmployeesCount,
    bool IsActive
);

public record CreateDepartmentDto(
    string Name,
    string? Code,
    string? Description,
    int? HeadEmployeeId
);

public record DepartmentLookupDto(
    int Id,
    string Name,
    string? Description,
    int ActiveJobCount,
    string? HeadEmployeeName
);
