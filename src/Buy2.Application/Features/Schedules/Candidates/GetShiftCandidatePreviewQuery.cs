using Buy2.Application.DTOs.Schedules;
using MediatR;

namespace Buy2.Application.Features.Schedules.Candidates;

public record GetShiftCandidatePreviewQuery(int CandidateId, int? SiteId = null) : IRequest<ShiftCandidatePreviewDto?>;
