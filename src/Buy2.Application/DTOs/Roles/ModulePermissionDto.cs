namespace Buy2.Application.DTOs.Roles;

/// <summary>
/// DTO representing a system module, granted actions, and access scope.
/// </summary>
public record ModulePermissionDto(string Module, List<string>? Actions, PermissionScopeDto? Scope);
