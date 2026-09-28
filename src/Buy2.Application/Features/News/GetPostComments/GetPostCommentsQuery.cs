using Buy2.Application.DTOs.News;
using Buy2.Application.DTOs.Requests;
using MediatR;

namespace Buy2.Application.Features.News.GetPostComments;

public record GetPostCommentsQuery(
    int PostId,
    int? CurrentUserId = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PaginatedListResult<CommentThreadDto>>;
