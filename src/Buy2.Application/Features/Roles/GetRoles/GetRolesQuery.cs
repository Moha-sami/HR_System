using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Roles;
using Buy2.Domain.Entities;
using Buy2.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

        IQueryable<Role> query = _roleRepository.Query().AsNoTracking().IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.Trim();
            query = query.Where(r => r.Name.Contains(searchTerm) || (r.Description != null && r.Description.Contains(searchTerm)));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(r => r.IsActive == filter.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = query.OrderByDescending(r => r.IsSystemRole).ThenBy(r => r.Name);

        var queryProjection = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                Role = r,
                EmployeeCount = r.Employees.Count(e => !e.IsDeleted)
            });

        var rolesList = await queryProjection.ToListAsync(cancellationToken);

        var items = rolesList.Select(r =>
        {
            var permissionsSummary = BuildPermissionsSummary(r.Role.PermissionsJson);
            return new RoleListItemDto(
                r.Role.Id,
                r.Role.Name,
                r.Role.Description,
                r.EmployeeCount,
                r.Role.IsSystemRole,
                r.Role.IsActive,
                r.Role.CreatedAt,
                permissionsSummary
            );
        }).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new RolePaginatedResponseDto(items, totalCount, page, pageSize, totalPages);
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
