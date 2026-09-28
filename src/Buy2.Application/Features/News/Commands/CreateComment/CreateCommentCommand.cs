using Buy2.Application.DTOs.News;
using MediatR;

namespace Buy2.Application.Features.News.Commands.CreateComment;

public record CreateCommentCommand(
    int PostId,
    string Content,
    int? ParentCommentId = null,
    int? AuthorId = null
) : IRequest<CommentDto>;
