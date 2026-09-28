using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.News;
using Buy2.Application.Features.News.Common;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.News.GetNewsPostDetail;

public class GetNewsPostDetailQueryHandler : IRequestHandler<GetNewsPostDetailQuery, NewsPostDetailDto>
{
    private readonly IRepository<Post> _postRepository;

    public GetNewsPostDetailQueryHandler(IRepository<Post> postRepository)
    {
        _postRepository = postRepository;
    }

    public async Task<NewsPostDetailDto> Handle(
        GetNewsPostDetailQuery request,
        CancellationToken cancellationToken)
    {
        var post = await _postRepository.Query(asNoTracking: true)
            .Include(p => p.Author)
            .Include(p => p.Comments)
            .Include(p => p.Reactions)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.PostType == "News", cancellationToken);

        if (post == null)
        {
            throw new KeyNotFoundException($"News post with ID {request.Id} was not found.");
        }

        var now = DateTime.UtcNow;

        // Visibility enforcement for non-elevated users
        if (!request.IsElevatedUser)
        {
            var isReleasedScheduled = string.Equals(post.Status, NewsLifecycleManager.StatusScheduled, StringComparison.OrdinalIgnoreCase) &&
                                      post.ScheduledFor.HasValue && post.ScheduledFor.Value <= now;

            var isPublished = string.Equals(post.Status, NewsLifecycleManager.StatusPublished, StringComparison.OrdinalIgnoreCase);

            if (!isPublished && !isReleasedScheduled)
            {
                throw new KeyNotFoundException($"News post with ID {request.Id} was not found.");
            }
        }

        var effectiveStatus = NewsLifecycleManager.ResolveEffectiveStatus(post.Status, post.ScheduledFor);
        var commentsCount = post.Comments.Count(c => !c.IsDeleted);
        var likesCount = post.Reactions.Count(r => r.ReactionType == "Like");

        var reactionBreakdown = post.Reactions
            .GroupBy(r => r.ReactionType)
            .ToDictionary(g => g.Key, g => g.Count());

        string? userReaction = null;
        if (request.CurrentUserId.HasValue)
        {
            userReaction = post.Reactions
                .FirstOrDefault(r => r.EmployeeId == request.CurrentUserId.Value)?
                .ReactionType;
        }

        var authorName = post.Author != null
            ? $"{post.Author.FirstName} {post.Author.LastName}".Trim()
            : "Corporate Admin";

        return new NewsPostDetailDto(
            Id: post.Id,
            Title: post.Title,
            Content: post.Content,
            Category: post.Category,
            Status: effectiveStatus,
            AuthorId: post.AuthorId,
            AuthorName: authorName,
            AuthorAvatar: null,
            MediaUrl: post.MediaUrl,
            ScheduledFor: post.ScheduledFor,
            PublishedAt: post.PublishedAt,
            LikesCount: likesCount,
            CommentsCount: commentsCount,
            ReactionBreakdown: reactionBreakdown,
            UserReaction: userReaction,
            CreatedAt: post.CreatedAt,
            UpdatedAt: post.UpdatedAt
        );
    }
}
