using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Schedules;
using MediatR;

namespace Buy2.Application.Features.Schedules.ApplyTemplate;

public record ApplyTemplateCommand(
    int SiteId,
    DateOnly Date,
    int TemplateId,
    string? Keep = null
) : IRequest<Result<ApplyTemplateResponseDto>>;
