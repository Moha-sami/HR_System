using System.Collections.Generic;
using Buy2.Application.DTOs.Schedules;
using MediatR;

namespace Buy2.Application.Features.Schedules.GetShiftCandidateEmployees;

public record GetShiftCandidateEmployeesQuery(
    string? Search = null,
    List<int>? RoleIds = null,
    List<int>? QualificationIds = null,
    decimal? MinHours = null,
    decimal? MaxHours = null,
    List<string>? RatingTiers = null,
    bool? IsPreferredOnly = null,
    int? SiteId = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<PaginatedShiftCandidateEmployeesResponseDto>;
