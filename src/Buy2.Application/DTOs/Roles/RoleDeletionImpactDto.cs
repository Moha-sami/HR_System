namespace Buy2.Application.DTOs.Roles;

public record AffectedEmployeeDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string Email,
    string JobRoleTitle,
    string SiteName
);

public record RoleDeletionImpactDto(
    int RoleId,
    string RoleName,
    bool IsSystemRole,
    bool CanDeleteDirectly,
    int AssignedEmployeesCount,
    List<AffectedEmployeeDto> AffectedEmployees
);
