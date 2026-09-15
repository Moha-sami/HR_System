using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.DTOs.Schedules;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Schedules.GetSiteShiftsOverview;

public class GetSiteShiftsOverviewQueryHandler : IRequestHandler<GetSiteShiftsOverviewQuery, SiteShiftsOverviewPaginatedResponseDto>
{
    private readonly IRepository<Site> _siteRepository;
    private readonly IRepository<ShiftEntity> _shiftRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public GetSiteShiftsOverviewQueryHandler(
        IRepository<Site> siteRepository,
        IRepository<ShiftEntity> shiftRepository,
        IRepository<Employee> employeeRepository)
    {
        _siteRepository = siteRepository;
        _shiftRepository = shiftRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<SiteShiftsOverviewPaginatedResponseDto> Handle(GetSiteShiftsOverviewQuery request, CancellationToken cancellationToken)
    {
        var today = DateTimeOffset.UtcNow.Date;
        var startUtc = new DateTimeOffset(today, TimeSpan.Zero);
        var endUtc = startUtc.AddDays(3);

        var sitesSpec = new Specification<Site>()
            .Include(nameof(Site.Region))
            .OrderBy(s => s.Id);

        if (request.RegionId.HasValue)
        {
            sitesSpec.Where(s => s.RegionId == request.RegionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            sitesSpec.Where(s => s.SiteName.ToLower().Contains(search) || s.Address.ToLower().Contains(search));
        }

        var sites = await _siteRepository.ListAsync(sitesSpec, cancellationToken);
        var totalCount = sites.Count;

        var siteIds = sites.Select(s => s.Id).ToList();

        var shifts = await _shiftRepository.ListAsync(
            s => siteIds.Contains(s.SiteId) && s.StartTime >= startUtc && s.StartTime < endUtc,
            cancellationToken);

        var assignedEmployeeIds = shifts
            .Where(s => s.EmployeeId.HasValue)
            .Select(s => s.EmployeeId!.Value)
            .Distinct()
            .ToList();

        var employeeList = await _employeeRepository.ListAsync(
            e => assignedEmployeeIds.Contains(e.Id),
            cancellationToken);

        var employees = employeeList.ToDictionary(e => e.Id);

        var items = new List<SiteShiftOverviewCardDto>();

        foreach (var site in sites)
        {
            var siteShifts = shifts.Where(s => s.SiteId == site.Id).ToList();

            var totalShifts = siteShifts.Count;
            var openShifts = siteShifts.Count(s => s.EmployeeId == null);
            var filledShifts = siteShifts.Count(s => s.EmployeeId != null);
            var assignedShifts = siteShifts.Where(s => s.EmployeeId != null).ToList();

            bool hasUnqualifiedAssignment = assignedShifts.Any(s =>
            {
                if (employees.TryGetValue(s.EmployeeId!.Value, out var emp))
                {
                    return emp.JobRoleId != s.JobRoleId;
                }
                return false;
            });

            bool hasShiftOver8Hours = assignedShifts.Any(s => (s.EndTime - s.StartTime).TotalHours > 8.0);

            bool hasMultipleShiftsInSameDay = assignedShifts
                .GroupBy(s => (s.EmployeeId, s.StartTime.UtcDateTime.Date))
                .Any(g => g.Count() > 1) ||
                assignedShifts
                .GroupBy(s => (s.EmployeeId, s.StartTime.Date))
                .Any(g => g.Count() > 1);

            bool hasOvertimeRisk = hasShiftOver8Hours || hasMultipleShiftsInSameDay;
            bool hasShortage = openShifts > 0;

            ShiftCoverageHealthStatus status;
            if (hasUnqualifiedAssignment)
            {
                status = ShiftCoverageHealthStatus.UnqualifiedAssignment;
            }
            else if (hasOvertimeRisk)
            {
                status = ShiftCoverageHealthStatus.OvertimeRisk;
            }
            else if (hasShortage)
            {
                status = ShiftCoverageHealthStatus.Shortage;
            }
            else
            {
                status = ShiftCoverageHealthStatus.Covered;
            }

            items.Add(new SiteShiftOverviewCardDto(
                site.Id,
                site.SiteName,
                site.Address,
                site.RegionId,
                site.Region?.Name ?? string.Empty,
                totalShifts,
                openShifts,
                filledShifts,
                status,
                site.IsSmartAssignmentEnabled,
                site.IsSmartPostingEnabled
            ));
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var pagedItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new SiteShiftsOverviewPaginatedResponseDto(pagedItems, totalCount, request.Page, request.PageSize);
    }
}
