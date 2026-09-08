using System.Collections.Generic;
using Buy2.Application.DTOs.Schedules;
using MediatR;

namespace Buy2.Application.Features.Schedules.Candidates;

public record GetShiftCandidatesQuery(
    string? Search = null,
    List<int>? JobRoleIds = null,
    List<string>? Qualifications = null,
    decimal? MinHours = null,
    decimal? MaxHours = null,
    decimal? MinRating = null,
    decimal? MaxRating = null,
    int? SiteId = null,
    bool? IsPreferredOnly = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<PaginatedShiftCandidatesResponseDto>;
