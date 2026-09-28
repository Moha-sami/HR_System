using MediatR;

namespace Buy2.Application.Features.News.Commands.ToggleReaction;

public record ToggleReactionCommand(
    string TargetType,
    int TargetId,
    string ReactionType,
    int? CallerId = null
) : IRequest<ReactionSummaryDto>;
