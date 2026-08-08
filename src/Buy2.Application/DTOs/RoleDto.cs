namespace Buy2.Application.DTOs;

public record RoleDto
{
    public string Name { get; init; } = string.Empty;
    public string Permissions { get; init; } = string.Empty;
}
