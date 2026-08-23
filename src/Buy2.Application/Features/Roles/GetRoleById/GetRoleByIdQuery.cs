using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Roles;
using Buy2.Domain.Entities;
using Buy2.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Buy2.Application.Features.Roles.GetRoleById;

public record GetRoleByIdQuery(int Id) : IRequest<RoleDetailsDto?>;

public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, RoleDetailsDto?>
{
    private readonly IRepository<Role> _roleRepository;

    public GetRoleByIdQueryHandler(IRepository<Role> roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<RoleDetailsDto?> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.Query()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(r => r.Employees)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (role is null)
        {
            return null;
        }

        var assignedCount = role.Employees?.Count(e => !e.IsDeleted) ?? 0;
        var permissions = ParseModulePermissions(role.PermissionsJson);

        return new RoleDetailsDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.IsActive,
            assignedCount,
            role.CreatedAt,
            role.UpdatedAt,
            permissions
        );
    }

    private static List<ModulePermissionDto> ParseModulePermissions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<ModulePermissionDto>();
        }

        var trimmed = json.Trim();
        if (trimmed == "[]" || trimmed == "{}")
        {
            return new List<ModulePermissionDto>();
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            if (trimmed.StartsWith("["))
            {
                var moduleDtos = JsonSerializer.Deserialize<List<ModulePermissionDto>>(trimmed, jsonOptions);
                if (moduleDtos != null && moduleDtos.Count > 0 && moduleDtos.All(m => m != null && !string.IsNullOrWhiteSpace(m.Module)))
                {
                    return moduleDtos;
                }
            }
        }
        catch
        {
            // Fall back to secondary parsing strategies
        }

        try
        {
            var doc = RolePermissionsDocument.FromJson(trimmed);
            if (doc != null && doc.Permissions.Count > 0)
            {
                return doc.Permissions.Select(p => new ModulePermissionDto(
                    p.Module.ToString(),
                    p.Actions.ToList(),
                    new PermissionScopeDto(p.Scope.ToString(), p.ScopeTargetIds.ToList())
                )).ToList();
            }
        }
        catch
        {
            // Fall back to secondary parsing strategies
        }

        try
        {
            if (trimmed.StartsWith("["))
            {
                var stringList = JsonSerializer.Deserialize<List<string>>(trimmed, jsonOptions);
                if (stringList != null)
                {
                    return stringList
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => new ModulePermissionDto(s, null, null))
                        .ToList();
                }
            }
        }
        catch
        {
            // Ignore deserialization exceptions
        }

        return new List<ModulePermissionDto>();
    }
}
