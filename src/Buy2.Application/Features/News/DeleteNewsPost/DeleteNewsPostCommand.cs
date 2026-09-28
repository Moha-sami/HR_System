using MediatR;

namespace Buy2.Application.Features.News.DeleteNewsPost;

public record DeleteNewsPostCommand(
    int Id,
    bool IsElevatedUser,
    int? CurrentUserId = null
) : IRequest<bool>;
