using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Sites;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Sites.GetSiteShiftsOverview;

public record GetSiteShiftsOverviewQuery(
    int? RegionId = null,
    string? Search = null,
    CoverageHealthStatus? CoverageStatus = null
) : IRequest<List<SiteShiftCoverageOverviewDto>>;

public class GetSiteShiftsOverviewQueryHandler : IRequestHandler<GetSiteShiftsOverviewQuery, List<SiteShiftCoverageOverviewDto>>
{
    private readonly IRepository<Site> _siteRepository;

    public GetSiteShiftsOverviewQueryHandler(IRepository<Site> siteRepository)
    {
        _siteRepository = siteRepository;
    }

    public async Task<List<SiteShiftCoverageOverviewDto>> Handle(GetSiteShiftsOverviewQuery request, CancellationToken cancellationToken)
    {
        var today = DateTimeOffset.UtcNow.Date;
        var startUtc = new DateTimeOffset(today, TimeSpan.Zero);
        var endUtc = startUtc.AddDays(3);

        var day1 = DateOnly.FromDateTime(today);
        var day2 = day1.AddDays(1);
        var day3 = day1.AddDays(2);

        var query = _siteRepository.Query(true)
            .Include(s => s.Region)
            .Include(s => s.Shifts)
            .AsQueryable();

        if (request.RegionId.HasValue)
        {
            query = query.Where(s => s.RegionId == request.RegionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(s => s.SiteName.ToLower().Contains(search) || s.Address.ToLower().Contains(search));
        }

        var sites = await query.ToListAsync(cancellationToken);

        var result = new List<SiteShiftCoverageOverviewDto>();

        foreach (var site in sites)
        {
            var siteShifts = site.Shifts
                .Where(s => (s.StartTime >= startUtc && s.StartTime < endUtc) ||
                            (s.StartTime.UtcDateTime >= startUtc.UtcDateTime && s.StartTime.UtcDateTime < endUtc.UtcDateTime))
                .ToList();

            var days = new List<DayCoverageStatusDto>
            {
                CalculateDayCoverage(day1, siteShifts),
                CalculateDayCoverage(day2, siteShifts),
                CalculateDayCoverage(day3, siteShifts)
            };

            if (request.CoverageStatus.HasValue && !days.Any(d => d.Status == request.CoverageStatus.Value))
            {
                continue;
            }

            result.Add(new SiteShiftCoverageOverviewDto(
                site.Id,
                site.SiteName,
                site.Region?.Name ?? string.Empty,
                site.Address,
                site.IsSmartAssignmentEnabled,
                site.IsSmartPostingEnabled,
                days
            ));
        }

        return result;
    }

    private static DayCoverageStatusDto CalculateDayCoverage(DateOnly targetDate, IEnumerable<ShiftEntity> shifts)
    {
        var dayShifts = shifts.Where(s =>
            DateOnly.FromDateTime(s.StartTime.Date) == targetDate ||
            DateOnly.FromDateTime(s.StartTime.UtcDateTime.Date) == targetDate
        ).ToList();

        int totalShifts = dayShifts.Count;
        int openShifts = dayShifts.Count(s => s.EmployeeId == null);
        int filledShifts = dayShifts.Count(s => s.EmployeeId != null);

        CoverageHealthStatus status;
        if (totalShifts == 0)
        {
            status = CoverageHealthStatus.NoShifts;
        }
        else if (openShifts == 0)
        {
            status = CoverageHealthStatus.Full;
        }
        else if ((double)filledShifts / totalShifts >= 0.5)
        {
            status = CoverageHealthStatus.Partial;
        }
        else
        {
            status = CoverageHealthStatus.Critical;
        }

        return new DayCoverageStatusDto(targetDate, status, totalShifts, openShifts, filledShifts);
    }
}
