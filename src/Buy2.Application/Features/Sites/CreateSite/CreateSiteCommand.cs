using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;
using System.Text.Json;

namespace Buy2.Application.Features.Sites.CreateSite;

public record CreateSiteCommand
    (
        string SiteName,
        decimal Latitude,
        decimal Longitude,
        string MacWhitelist
    ) : IRequest<int>;

public class CreateSiteCommandHandler : IRequestHandler<CreateSiteCommand, int>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSiteCommandHandler(IRepository<Site> siteRepository, IUnitOfWork unitOfWork)
    {
        _siteRepository = siteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateSiteCommand command, CancellationToken cancellationToken)
    {
        var site = new Site
        {
            SiteName = command.SiteName,
            Latitude = (double)command.Latitude,
            Longitude = (double)command.Longitude,
            MacAddress = command.MacWhitelist
        };

        await _siteRepository.AddAsync(site);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return site.Id;
    }
}

