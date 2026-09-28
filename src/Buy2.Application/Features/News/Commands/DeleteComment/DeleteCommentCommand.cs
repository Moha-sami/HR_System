using MediatR;

namespace Buy2.Application.Features.News.Commands.DeleteComment;

public record DeleteCommentCommand(
    int CommentId,
    int? CallerId = null,
    bool IsElevatedUser = false,
    string? ModerationReason = null
) : IRequest<bool>;
