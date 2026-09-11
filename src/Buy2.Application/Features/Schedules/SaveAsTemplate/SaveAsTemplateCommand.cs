using Buy2.Application.Common.Models;
using Buy2.Application.Features.Sites.GetSiteShiftTemplates;
using MediatR;

namespace Buy2.Application.Features.Schedules.SaveAsTemplate;

public record SaveAsTemplateCommand(
    int SiteId,
    DateOnly Date,
    string? Name,
    int? ActorEmployeeId
) : IRequest<Result<SiteShiftTemplateDto>>;
