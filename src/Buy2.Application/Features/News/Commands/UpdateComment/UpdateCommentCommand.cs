using Buy2.Application.DTOs.News;
using MediatR;

namespace Buy2.Application.Features.News.Commands.UpdateComment;

public record UpdateCommentCommand(
    int CommentId,
    string Content,
    int? CallerId = null
) : IRequest<CommentDto>;
