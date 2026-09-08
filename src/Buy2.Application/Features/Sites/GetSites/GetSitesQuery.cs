using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Sites.GetSites;

public record GetSitesQuery() : IRequest<List<SiteDto>>;

public class GetSitesQueryHandler : IRequestHandler<GetSitesQuery, List<SiteDto>>
{
    private readonly IRepository<Site> _siteRepository;

    public GetSitesQueryHandler(IRepository<Site> siteRepository)
    {
        _siteRepository = siteRepository;
    }

    public async Task<List<SiteDto>> Handle(GetSitesQuery request, CancellationToken cancellationToken)
    {
        var sites = await _siteRepository.GetAllAsync(cancellationToken);
        return sites.Select(s => new SiteDto(
            s.Id,
            s.SiteName,
            s.Latitude,
            s.Longitude,
            new List<string>()
        )).ToList();
    }
}
