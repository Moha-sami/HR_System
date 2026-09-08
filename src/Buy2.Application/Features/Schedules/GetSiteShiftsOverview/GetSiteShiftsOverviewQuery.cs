using Buy2.Application.DTOs.Schedules;
using MediatR;

namespace Buy2.Application.Features.Schedules.GetSiteShiftsOverview;

public record GetSiteShiftsOverviewQuery(
    string? Search = null,
    int? RegionId = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<SiteShiftsOverviewPaginatedResponseDto>;
