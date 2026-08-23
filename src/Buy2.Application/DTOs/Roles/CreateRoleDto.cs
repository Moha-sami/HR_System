namespace Buy2.Application.DTOs.Roles;

/// <summary>
/// DTO payload for creating a new role.
/// </summary>
public record CreateRoleDto(string Name, string? Description, List<ModulePermissionDto>? Permissions);
