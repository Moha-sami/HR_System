using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Buy2.Application.Features.Sites.CreateSite;
public record CreateSiteCommand
    (
        string SiteName,
        double Latitude,
        double Longitude,
        List<string> MacWhitelist,
        string MacAddress,
        string Address,
        string MapUrl,
        string PhoneNumber,
        string Instructions,
        int RegionId,
        int MaxCapacity,
        List<int> PreferredEmployeeIds,
        List<SiteOperationalHourDto> OperationalHours
    ) : IRequest<int>;

public class CreateSiteCommandHandler : IRequestHandler<CreateSiteCommand, int>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<Region> _regionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSiteCommandHandler(IRepository<Site> siteRepository, IRepository<Region> regionRepository, IUnitOfWork unitOfWork)
    {
        _siteRepository = siteRepository;
        _regionRepository = regionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateSiteCommand command, CancellationToken cancellationToken)
    {
        var siteName = command.SiteName.Trim();
        var existingSite = await _siteRepository.
            AnyAsync(s => s.SiteName.ToLower() == siteName.ToLower()
            , cancellationToken);

        if (existingSite)
        {
            throw new ValidationException("A site with this name already exists");
        }

        var existingRegion = await _regionRepository
            .AnyAsync(r => r.Id == command.RegionId, cancellationToken);

        if (!existingRegion)
        {
            throw new ValidationException("Selected region does not exist.");
        }

        var preferredEmployee = command.PreferredEmployeeIds
            .Select(eId => new SitePreferredEmployee
            {
                EmployeeId = eId
            }).ToList();
        var operationalHours = command.OperationalHours
            .Select(houre => new SiteOperationalHour
            {
                DayOfWeek = houre.Day,
                IsOpen = houre.IsOpen,
                OpenTime = houre.From,
                CloseTime = houre.To
            }).ToList();

        var site = new Site
        {
            SiteName = siteName,
            Latitude = command.Latitude,
            Longitude = command.Longitude,
            MacAddressWhitelistJson = JsonSerializer.Serialize(command.MacWhitelist),
            MacAddress = command.MacAddress,
            Address = command.Address,
            MapUrl = command.MapUrl,
            PhoneNumber = command.PhoneNumber,
            Instructions = command.Instructions,
            RegionId = command.RegionId,
            MaxCapacity = command.MaxCapacity,
            PreferredEmployees = preferredEmployee,
            OperationalHours = operationalHours
        };

        await _siteRepository.AddAsync(site);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return site.Id;
    }
}

