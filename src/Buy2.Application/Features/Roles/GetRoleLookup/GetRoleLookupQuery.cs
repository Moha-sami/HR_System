using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.DTOs.Roles;
using Buy2.Domain.Entities;
using MediatR;

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
        var spec = new Specification<Role>()
            .Where(r => r.IsActive);

        if (request.ExcludeRoleId.HasValue)
        {
            var excludedId = request.ExcludeRoleId.Value;
            spec.Where(r => r.Id != excludedId);
        }

        spec.OrderBy(r => r.Name);

        return await _roleRepository.ListAsync(
            spec,
            r => new RoleLookupItemDto(r.Id, r.Name),
            cancellationToken);
    }
}
