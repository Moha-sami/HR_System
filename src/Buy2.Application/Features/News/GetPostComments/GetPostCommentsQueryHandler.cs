using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.News;
using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.News.Common;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.News.GetPostComments;

public class GetPostCommentsQueryHandler : IRequestHandler<GetPostCommentsQuery, PaginatedListResult<CommentThreadDto>>
{
    private readonly IRepository<Post> _postRepository;
    private readonly IRepository<Comment> _commentRepository;

    public GetPostCommentsQueryHandler(
        IRepository<Post> postRepository,
        IRepository<Comment> commentRepository)
    {
        _postRepository = postRepository;
        _commentRepository = commentRepository;
    }

    public async Task<PaginatedListResult<CommentThreadDto>> Handle(
        GetPostCommentsQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Verify post exists
        var postExists = await _postRepository.Query(asNoTracking: true)
            .AnyAsync(p => p.Id == request.PostId && p.PostType == "News", cancellationToken);

        if (!postExists)
        {
            throw new KeyNotFoundException($"News post with ID {request.PostId} was not found.");
        }

        // 2. Fetch all non-deleted comments and replies for this post
        var comments = await _commentRepository.Query(asNoTracking: true)
            .Include(c => c.Author)
            .Include(c => c.Reactions)
            .Where(c => c.PostId == request.PostId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        // 3. Separate root comments vs replies
        var rootComments = comments
            .Where(c => c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        var repliesLookup = comments
            .Where(c => c.ParentCommentId != null)
            .GroupBy(c => c.ParentCommentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.CreatedAt).ToList());

        // 4. Pagination on root parent comments
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;
        var totalCount = rootComments.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedRoots = rootComments
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // 5. Map to CommentThreadDto
        var items = pagedRoots.Select(parent =>
        {
            var repliesList = repliesLookup.TryGetValue(parent.Id, out var directReplies)
                ? directReplies
                : new List<Comment>();

            var replyDtos = repliesList.Select(r =>
            {
                var rAuthorName = r.Author != null
                    ? $"{r.Author.FirstName} {r.Author.LastName}".Trim()
                    : "Unknown User";

                var rLikesCount = r.Reactions.Count(react => react.ReactionType == SocialEngagementManager.ReactionLike);
                var rBreakdown = r.Reactions
                    .GroupBy(react => react.ReactionType)
                    .ToDictionary(g => g.Key, g => g.Count());

                string? rUserReaction = null;
                if (request.CurrentUserId.HasValue)
                {
                    rUserReaction = r.Reactions
                        .FirstOrDefault(react => react.EmployeeId == request.CurrentUserId.Value)?
                        .ReactionType;
                }

                var rContent = SocialEngagementManager.ResolveCommentDisplayContent(
                    r.Content,
                    r.IsModerated,
                    r.IsDeleted
                );

                return new CommentReplyDto(
                    Id: r.Id,
                    PostId: r.PostId,
                    ParentCommentId: r.ParentCommentId,
                    AuthorId: r.AuthorId,
                    AuthorName: rAuthorName,
                    AuthorAvatar: null,
                    Content: rContent,
                    IsModerated: r.IsModerated,
                    ModerationReason: r.ModerationReason,
                    LikesCount: rLikesCount,
                    ReactionBreakdown: rBreakdown,
                    UserReaction: rUserReaction,
                    CreatedAt: r.CreatedAt,
                    UpdatedAt: r.UpdatedAt
                );
            }).ToList();

            var parentAuthorName = parent.Author != null
                ? $"{parent.Author.FirstName} {parent.Author.LastName}".Trim()
                : "Unknown User";

            var parentLikesCount = parent.Reactions.Count(react => react.ReactionType == SocialEngagementManager.ReactionLike);
            var parentBreakdown = parent.Reactions
                .GroupBy(react => react.ReactionType)
                .ToDictionary(g => g.Key, g => g.Count());

            string? parentUserReaction = null;
            if (request.CurrentUserId.HasValue)
            {
                parentUserReaction = parent.Reactions
                    .FirstOrDefault(react => react.EmployeeId == request.CurrentUserId.Value)?
                    .ReactionType;
            }

            var parentContent = SocialEngagementManager.ResolveCommentDisplayContent(
                parent.Content,
                parent.IsModerated,
                parent.IsDeleted
            );

            return new CommentThreadDto(
                Id: parent.Id,
                PostId: parent.PostId,
                ParentCommentId: null,
                AuthorId: parent.AuthorId,
                AuthorName: parentAuthorName,
                AuthorAvatar: null,
                Content: parentContent,
                IsModerated: parent.IsModerated,
                ModerationReason: parent.ModerationReason,
                LikesCount: parentLikesCount,
                RepliesCount: replyDtos.Count,
                ReactionBreakdown: parentBreakdown,
                UserReaction: parentUserReaction,
                Replies: replyDtos,
                CreatedAt: parent.CreatedAt,
                UpdatedAt: parent.UpdatedAt
            );
        }).ToList();

        return new PaginatedListResult<CommentThreadDto>(
            items,
            totalCount,
            pageNumber,
            pageSize,
            totalPages
        );
    }
}
