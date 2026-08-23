namespace Buy2.Application.DTOs.Roles;

public record RoleDeletionResultDto(
    bool Success,
    int DeletedRoleId,
    int ReassignedEmployeesCount,
    string Message
);
