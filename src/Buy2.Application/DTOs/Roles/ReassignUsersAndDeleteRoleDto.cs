namespace Buy2.Application.DTOs.Roles;

public record EmployeeReassignmentDto(int EmployeeId, int NewRoleId);

public record ReassignUsersAndDeleteRoleDto(
    int? DefaultNewRoleId = null,
    List<EmployeeReassignmentDto>? Reassignments = null
);
