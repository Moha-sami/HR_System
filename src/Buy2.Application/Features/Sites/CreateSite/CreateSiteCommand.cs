using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSiteCommandHandler(IRepository<Site> siteRepository, IRepository<Region> regionRepository, IRepository<Employee> employeeRepository, IUnitOfWork unitOfWork)
    {
        _siteRepository = siteRepository;
        _regionRepository = regionRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateSiteCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.SiteName))
        {
            throw new ValidationException("Site name is required.");
        }

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
        var preferredEmployeeId = (command.PreferredEmployeeIds ?? new List<int>())
            .Distinct()
            .ToList();

        var existingPreferredEmployee = await _employeeRepository
            .Query()
            .CountAsync(e => preferredEmployeeId.Contains(e.Id), cancellationToken);

        if (existingPreferredEmployee != preferredEmployeeId.Count)
        {
            throw new ValidationException("One or more selected employees do not exist.");
        }

        var preferredEmployee = preferredEmployeeId
            .Select(eId => new SitePreferredEmployee
            {
                EmployeeId = eId
            }).ToList();



        var operationalHoursData = command.OperationalHours ?? new List<SiteOperationalHourDto>();

        var duplicatDay = operationalHoursData
            .GroupBy(e => e.Day)
            .Any(g => g.Count() > 1);

        if (duplicatDay)
        {
            throw new ValidationException("Operational hours cannot contain duplicate days.");
        }

        var operationalHours = operationalHoursData
            .Select(houre => new SiteOperationalHour
            {
                DayOfWeek = houre.Day,
                IsOpen = houre.IsOpen,
                OpenTime = houre.From,
                CloseTime = houre.To
            }).ToList();

        var macWhiteList = command.MacWhitelist ?? new List<string>();
        var site = new Site
        {
            SiteName = siteName,
            Latitude = command.Latitude,
            Longitude = command.Longitude,
            MacAddressWhitelistJson = JsonSerializer.Serialize(macWhiteList),
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

        await _siteRepository.AddAsync(site, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return site.Id;
    }
}