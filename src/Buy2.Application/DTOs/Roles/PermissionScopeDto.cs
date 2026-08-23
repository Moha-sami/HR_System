namespace Buy2.Application.DTOs.Roles;

/// <summary>
/// DTO representing permission access scope type and target entity IDs.
/// </summary>
public record PermissionScopeDto(string ScopeType, List<int>? TargetIds);
