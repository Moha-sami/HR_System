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
    private readonly IRepository<ShiftBlock> _shiftBlockRepository;

    public GetSiteShiftsQueryHandler(IRepository<Site> siteRepository, IRepository<ShiftEntity> shiftRepository, IRepository<ShiftBlock> shiftBlockRepository)
    {
        _siteRepository = siteRepository;
        _shiftRepository = shiftRepository;
        _shiftBlockRepository = shiftBlockRepository;
    }

    public async Task<List<ShiftTabDto>> Handle( GetSiteShiftsQuery query, CancellationToken cancellation)
    {
        var siteExists = await _siteRepository
            .Query()
            .AsNoTracking()
            .AnyAsync(
                s => s.Id == query.Id,
                cancellation
            );

        if (!siteExists)
        {
            throw new KeyNotFoundException("Site not found.");
        }

        var shifts = await _shiftRepository
            .Query()
            .AsNoTracking()
            .Where(s => s.SiteId == query.Id)
            .Include(s => s.JobRole)
            .Include(s => s.ShiftTemplate)
            .ToListAsync(cancellation);

        var templateIds = shifts
            .Where(s => s.ShiftTemplateId.HasValue)
            .Select(s => s.ShiftTemplateId!.Value)
            .Distinct()
            .ToList();

        var blockHeadcounts = templateIds.Count == 0
            ? new Dictionary<(int TemplateId, int JobRoleId), int>()
            : await _shiftBlockRepository
                .Query()
                .AsNoTracking()
                .Where(b => templateIds.Contains(b.ShiftTemplateId))
                .GroupBy(b => new { b.ShiftTemplateId, b.JobRoleId })
                .Select(g => new { g.Key.ShiftTemplateId, g.Key.JobRoleId, Count = g.Count() })
                .ToDictionaryAsync(
                    x => (x.ShiftTemplateId, x.JobRoleId),
                    x => x.Count,
                    cancellation);

        return shifts
            .Select(s =>
            {
                int headcount = 1;
                if (s.ShiftTemplateId.HasValue
                    && blockHeadcounts.TryGetValue((s.ShiftTemplateId.Value, s.JobRoleId), out var c))
                {
                    headcount = c;
                }

                return new ShiftTabDto(
                    s.Id,
                    s.ShiftTemplate?.Name ?? string.Empty,
                    TimeOnly.FromDateTime(s.StartTime.DateTime),
                    TimeOnly.FromDateTime(s.EndTime.DateTime),
                    s.IsPublished,
                    new List<ShiftRoleHeadcountDto>
                    {
                        new ShiftRoleHeadcountDto(
                            s.JobRole?.Title ?? string.Empty,
                            headcount
                        )
                    }
                );
            }).ToList();
    }
}