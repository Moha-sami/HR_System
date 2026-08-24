namespace Buy2.Application.DTOs.Roles;

public record RoleListItemDto(
    int Id,
    string Name,
    string? Description,
    int AssignedEmployeesCount,
    bool IsSystemRole,
    bool IsActive,
    DateTimeOffset CreatedAt,
    List<string> PermissionsSummary
);

public record RoleDetailsDto(
    int Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    bool IsActive,
    int AssignedEmployeesCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    List<ModulePermissionDto> Permissions
);

public record RoleFilterQueryDto(
    string? SearchTerm = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 10
);

public record RolePaginatedResponseDto(
    List<RoleListItemDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);
