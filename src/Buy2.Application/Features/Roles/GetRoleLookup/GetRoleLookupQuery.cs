using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Roles;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Roles.GetRoleLookup;

public record GetRoleLookupQuery(int? ExcludeRoleId = null) : IRequest<List<RoleLookupItemDto>>;

public class GetRoleLookupQueryHandler : IRequestHandler<GetRoleLookupQuery, List<RoleLookupItemDto>>
{
    private readonly IRepository<Role> _roleRepository;

    public GetRoleLookupQueryHandler(IRepository<Role> roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<List<RoleLookupItemDto>> Handle(GetRoleLookupQuery request, CancellationToken cancellationToken)
    {
        var query = _roleRepository.Query(asNoTracking: true)
            .Where(r => r.IsActive);

        if (request.ExcludeRoleId.HasValue)
        {
            query = query.Where(r => r.Id != request.ExcludeRoleId.Value);
        }

        return await query
            .OrderBy(r => r.Name)
            .Select(r => new RoleLookupItemDto(r.Id, r.Name))
            .ToListAsync(cancellationToken);
    }
}
