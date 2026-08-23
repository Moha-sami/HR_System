namespace Buy2.Application.DTOs.Roles;

public record UserRoleReassignmentItemDto(
    int EmployeeId,
    int NewRoleId
);

public record ReassignUsersAndDeleteRoleDto(
    int RoleId,
    List<UserRoleReassignmentItemDto>? Reassignments = null,
    int? DefaultNewRoleId = null
);
