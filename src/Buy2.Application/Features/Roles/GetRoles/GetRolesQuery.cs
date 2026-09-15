using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.DTOs.Roles;
using Buy2.Domain.Entities;
using Buy2.Domain.ValueObjects;
using MediatR;
using System.Text.Json;

namespace Buy2.Application.Features.Roles.GetRoles;

public record GetRolesQuery(RoleFilterQueryDto Filter) : IRequest<RolePaginatedResponseDto>;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, RolePaginatedResponseDto>
{
    private readonly IRepository<Role> _roleRepository;

    public GetRolesQueryHandler(IRepository<Role> roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<RolePaginatedResponseDto> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter ?? new RoleFilterQueryDto();

        var page = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var spec = new Specification<Role>()
            .IgnoreFilters()
            .Include(nameof(Role.Employees));

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.Trim();
            spec.Where(r => r.Name.Contains(searchTerm) || (r.Description != null && r.Description.Contains(searchTerm)));
        }

        if (filter.IsActive.HasValue)
        {
            var isActive = filter.IsActive.Value;
            spec.Where(r => r.IsActive == isActive);
        }

        spec.OrderBy(r => r.IsSystemRole, descending: true).ThenBy(r => r.Name);

        var paged = await _roleRepository.PagedAsync(spec, page, pageSize, cancellationToken);

        var items = paged.Items.Select(r =>
        {
            var permissionsSummary = BuildPermissionsSummary(r.PermissionsJson);
            var employeeCount = r.Employees != null ? r.Employees.Count(e => !e.IsDeleted) : 0;
            return new RoleListItemDto(
                r.Id,
                r.Name,
                r.Description,
                employeeCount,
                r.IsSystemRole,
                r.IsActive,
                r.CreatedAt,
                permissionsSummary
            );
        }).ToList();

        return new RolePaginatedResponseDto(items, paged.TotalCount, paged.PageNumber, paged.PageSize, paged.TotalPages);
    }

    private static List<string> BuildPermissionsSummary(string permissionsJson)
    {
        if (string.IsNullOrWhiteSpace(permissionsJson))
        {
            return new List<string>();
        }

        var trimmed = permissionsJson.Trim();

        try
        {
            if (trimmed.StartsWith("["))
            {
                var stringList = JsonSerializer.Deserialize<List<string>>(trimmed);
                if (stringList != null && stringList.All(s => s is string))
                {
                    return stringList;
                }
            }
        }
        catch
        {
            // Fall back to structured RolePermissionsDocument parsing if simple string array deserialization failed
        }

        try
        {
            var doc = RolePermissionsDocument.FromJson(trimmed);
            if (doc.Permissions.Count > 0)
            {
                return doc.Permissions.Select(p => p.Module.ToString()).ToList();
            }
        }
        catch
        {
            // Fallback to empty list on parsing failure
        }

        return new List<string>();
    }
}
