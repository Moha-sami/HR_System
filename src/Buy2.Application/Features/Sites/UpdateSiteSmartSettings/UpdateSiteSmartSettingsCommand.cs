using Buy2.Application.DTOs.Sites;
using MediatR;

namespace Buy2.Application.Features.Sites.UpdateSiteSmartSettings;

public record UpdateSiteSmartSettingsCommand(
    int SiteId,
    bool? IsSmartAssignmentEnabled,
    bool? IsSmartPostingEnabled
) : IRequest<SiteSmartSettingsResponseDto>;
