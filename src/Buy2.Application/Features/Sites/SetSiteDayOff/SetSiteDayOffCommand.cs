using Buy2.Application.DTOs.Sites;
using MediatR;

namespace Buy2.Application.Features.Sites.SetSiteDayOff;

public record SetSiteDayOffCommand(
    int SiteId,
    DateOnly Date,
    bool IsDayOff
) : IRequest<SetSiteDayOffResponseDto>;
