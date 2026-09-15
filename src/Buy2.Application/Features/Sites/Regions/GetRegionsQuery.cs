using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using MediatR;
namespace Buy2.Application.Features.Sites.Regions;

public record GetRegionsQuery() : IRequest<List<RegionListItemDto>>;
public class GetRegionsQueryHandler : IRequestHandler<GetRegionsQuery, List<RegionListItemDto>>
{
    private readonly IRepository<Region> _regionRepository;
    public GetRegionsQueryHandler(IRepository<Region> regionRepository)
    {
        _regionRepository = regionRepository;
    }
    public async Task<List<RegionListItemDto>> Handle(GetRegionsQuery getRegionsQuery, CancellationToken cancellationToken)
    {
        var spec = new Specification<Region>()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name);
        return await _regionRepository
            .ListAsync(spec, r => new RegionListItemDto(r.Id, r.Name), cancellationToken);
    }
}