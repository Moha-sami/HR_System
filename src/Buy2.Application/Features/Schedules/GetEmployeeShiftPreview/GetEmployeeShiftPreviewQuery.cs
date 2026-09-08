using Buy2.Application.DTOs.Schedules;
using MediatR;

namespace Buy2.Application.Features.Schedules.GetEmployeeShiftPreview;

public record GetEmployeeShiftPreviewQuery(int EmployeeId, int? SiteId = null) : IRequest<ShiftCandidatePreviewDto?>;
