using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
        return await _regionRepository
            .Query()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new RegionListItemDto(r.Id, r.Name))
            .ToListAsync(cancellationToken);
    }
}