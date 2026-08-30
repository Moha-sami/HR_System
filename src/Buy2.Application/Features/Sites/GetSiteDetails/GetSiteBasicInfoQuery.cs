using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Buy2.Application.Features.Sites.GetSiteDetails;

public record GetSiteBasicInfoQuery(int Id) : IRequest<SiteFullProfile>;

public class GetSiteBasicInfoQueryHandler : IRequestHandler<GetSiteBasicInfoQuery ,SiteFullProfile>
{
    private readonly IRepository<Site> _siteRepository;
    public GetSiteBasicInfoQueryHandler(IRepository<Site> site)
    {
        _siteRepository = site;
    }

    public async Task<SiteFullProfile> Handle(GetSiteBasicInfoQuery query, CancellationToken cancellation)
    {
        var site = await _siteRepository
            .Query(false)
            .Include(s => s.Region)
            .Include(s => s.PreferredEmployees)
                .ThenInclude(p => p.Employee)
                    .ThenInclude(p => p.JobRole)
            .Include(s => s.Documents)
            .Include(s => s.OperationalHours)
            .FirstOrDefaultAsync(s=> s.Id == query.Id ,cancellation);
        if (site is null)
        {
            throw new KeyNotFoundException("Site is not found.");
        }

        var preferredEmployee = site.PreferredEmployees
            .Select(p => new PreferredPersonDto(
                $"{p.Employee?.FirstName ?? string.Empty} "+
                $"{p.Employee?.LastName ?? string.Empty}".Trim(),
                p.Employee?.JobRole?.Title ?? string.Empty
                )).ToList();
        var documents = site.Documents
            .Select(d => new DocumentDto(
                d.Id, d.FileName, d.FilePath
                )).ToList();
        var operationalHours = site.OperationalHours
            .Select(o => new OperationalScheduleDto(
                o.DayOfWeek, o.OpenTime, o.CloseTime
                )).ToList();
        return new SiteFullProfile(
            site.SiteName,
            site.Region?.Name ?? string.Empty,
            site.Address,
            site.MacAddress,
            site.MapUrl,
            site.PhoneNumber,
            site.Instructions,
            preferredEmployee,
            documents,
            operationalHours
            );
    }
}