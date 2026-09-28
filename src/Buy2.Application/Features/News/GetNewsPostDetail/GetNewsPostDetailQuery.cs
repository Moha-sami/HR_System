using Buy2.Application.DTOs.News;
using MediatR;

namespace Buy2.Application.Features.News.GetNewsPostDetail;

public record GetNewsPostDetailQuery(
    int Id,
    int? CurrentUserId = null,
    bool IsElevatedUser = false
) : IRequest<NewsPostDetailDto>;
