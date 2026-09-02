using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Sites.GetSiteDetails;

public record GetSiteShiftsQuery(int Id) : IRequest<List<ShiftTabDto>>;

public class GetSiteShiftsQueryHandler : IRequestHandler<GetSiteShiftsQuery, List<ShiftTabDto>>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<ShiftEntity> _shiftRepository;

    public GetSiteShiftsQueryHandler( IRepository<Site> siteRepository, IRepository<ShiftEntity> shiftRepository)
    {
        _siteRepository = siteRepository;
        _shiftRepository = shiftRepository;
    }

    public async Task<List<ShiftTabDto>> Handle( GetSiteShiftsQuery query, CancellationToken cancellation)
    {
        var siteExists = await _siteRepository
            .Query(false)
            .AnyAsync(
                s => s.Id == query.Id,
                cancellation);

        if (!siteExists)
        {
            throw new KeyNotFoundException("Site not found.");
        }

        var shifts = await _shiftRepository
            .Query(false)
            .Where(s => s.SiteId == query.Id)
            .Include(s => s.JobRole)
            .Include(s => s.ShiftTemplate)
            .ToListAsync(cancellation);

        return shifts
            .Select(s => new ShiftTabDto(
                s.Id,
                s.ShiftTemplate?.Name ?? string.Empty,
                TimeOnly.FromDateTime(s.StartTime.DateTime),
                TimeOnly.FromDateTime(s.EndTime.DateTime),
                s.IsPublished,
                new List<ShiftRoleHeadcountDto>
                {
                    new ShiftRoleHeadcountDto(
                        s.JobRole?.Title ?? string.Empty,
                        s.ShiftTemplate?.RequiredHeadcount ?? 0
                    )
                }
            )).ToList();
    }
}