namespace Buy2.Application.DTOs.Roles;

/// <summary>
/// DTO payload for modifying an existing role.
/// </summary>
public record UpdateRoleDto(string Name, string? Description, bool IsActive, List<ModulePermissionDto>? Permissions);
