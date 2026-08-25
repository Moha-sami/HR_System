using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Buy2.Application.Features.Sites.UpdateSite;

public record UpdateSiteCommand(
    int Id,
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

public class UpdateSiteCommandHandler : IRequestHandler<UpdateSiteCommand, int>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<Region> _regionRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateSiteCommandHandler(IRepository<Site> siteRepository, IRepository<Region> regionRepository, IRepository<Employee> employeeRepository, IUnitOfWork unitOfWork)
    {
        _siteRepository = siteRepository;
        _regionRepository = regionRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }
    public async Task<int> Handle(UpdateSiteCommand command, CancellationToken cancellation)
    {
        var site = await _siteRepository
            .Query(false)
            .Include(s => s.OperationalHours)
            .Include(s => s.PreferredEmployees)
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellation);
        if (site is null)
        {
            throw new KeyNotFoundException("Site not found.");
        }
        var siteName = command.SiteName.Trim();
        var existingSiteName = await _siteRepository
            .AnyAsync(s => s.Id != command.Id && s.SiteName.ToLower() == siteName.ToLower(),
            cancellation);
        if (existingSiteName)
        {
            throw new ValidationException("A site with this name already exists.");
        }

        var getRegion = await _regionRepository.GetByIdAsync(command.RegionId);
        if (getRegion is null)
        {
            throw new ValidationException("Selected region does not exist.");
        }

        site.SiteName = siteName;
        site.Latitude = command.Latitude;
        site.Longitude = command.Longitude;
        site.MacAddressWhitelistJson = JsonSerializer.Serialize(command.MacWhitelist);
        site.MacAddress = command.MacAddress;
        site.Address = command.Address;
        site.MapUrl = command.MapUrl;
        site.PhoneNumber = command.PhoneNumber;
        site.Instructions = command.Instructions;
        site.RegionId = command.RegionId;
        site.MaxCapacity = command.MaxCapacity;

        var employeeCommand = command.PreferredEmployeeIds.Distinct().ToList();
        if (employeeCommand.Count() > 0)
        {
            var employeeExistsCount = await _employeeRepository
                .Query()
                .CountAsync(e => employeeCommand.Contains(e.Id), cancellation);
            if (employeeExistsCount != employeeCommand.Count)
            {
                throw new ValidationException("One or more selected employees do not exist.");
            }
        }

        site.PreferredEmployees.Clear();
        foreach (var employee in employeeCommand)
        {
            site.PreferredEmployees.Add(new SitePreferredEmployee
            {
                EmployeeId = employee
            });
        }

        var duplicateDay = command.OperationalHours
            .GroupBy(o => o.Day)
            .Any(g => g.Count() > 1);
        if (duplicateDay)
        {
            throw new ValidationException("Invalid operational hours.");
        }

        site.OperationalHours.Clear();
        foreach (var op in command.OperationalHours)
        {
            site.OperationalHours.Add(new SiteOperationalHour
            {
                DayOfWeek = op.Day,
                IsOpen = op.IsOpen,
                OpenTime = op.From,
                CloseTime = op.To,
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellation);

        return site.Id;
    }
}