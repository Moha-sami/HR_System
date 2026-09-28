using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.News.Common;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.News.Commands.DeleteComment;

public class DeleteCommentHandler : IRequestHandler<DeleteCommentCommand, bool>
{
    private readonly IRepository<Comment> _commentRepository;
    private readonly IRepository<Post> _postRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommentHandler(
        IRepository<Comment> commentRepository,
        IRepository<Post> postRepository,
        IUnitOfWork unitOfWork)
    {
        _commentRepository = commentRepository;
        _postRepository = postRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        if (request.CommentId <= 0)
        {
            throw new ArgumentException("Comment ID must be greater than 0.", nameof(request.CommentId));
        }

        var comment = await _commentRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken);

        if (comment == null || comment.IsDeleted)
        {
            throw new KeyNotFoundException($"Comment with ID {request.CommentId} was not found.");
        }

        if (comment.Content == SocialEngagementManager.TombstoneDeletedComment)
        {
            throw new InvalidOperationException("This comment has already been deleted.");
        }

        var isAuthor = request.CallerId.HasValue && request.CallerId.Value == comment.AuthorId;
        if (!request.IsElevatedUser && !isAuthor)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete or moderate this comment.");
        }

        var now = DateTime.UtcNow;

        if (request.IsElevatedUser)
        {
            // AC 2: If caller is an Administrator or Moderator, comment is marked with tombstone flag: "This comment has been removed by admin"
            if (comment.IsModerated)
            {
                throw new InvalidOperationException("This comment has already been moderated.");
            }

            comment.IsModerated = true;
            comment.ModerationReason = string.IsNullOrWhiteSpace(request.ModerationReason)
                ? "Removed by administrator"
                : request.ModerationReason.Trim();
            comment.Content = SocialEngagementManager.TombstoneModeratedComment;
            comment.UpdatedAt = now;

            _commentRepository.Update(comment);
        }
        else
        {
            // Caller is Author
            if (comment.IsModerated)
            {
                throw new InvalidOperationException("Cannot delete a comment that has been removed by an administrator.");
            }

            var hasChildReplies = await _commentRepository.Query(asNoTracking: true)
                .AnyAsync(c => c.ParentCommentId == comment.Id && !c.IsDeleted, cancellationToken);

            if (hasChildReplies)
            {
                // AC 3: If comment has nested child replies, content is tombstoned to prevent breaking conversation continuity
                comment.Content = SocialEngagementManager.TombstoneDeletedComment;
                comment.UpdatedAt = now;
                _commentRepository.Update(comment);
            }
            else
            {
                // AC 1: If caller is original author with no replies, comment is removed (soft delete)
                comment.IsDeleted = true;
                comment.DeletedAt = now;
                _commentRepository.Update(comment);
            }
        }

        // AC 4: Decrements active comments count appropriately
        var post = await _postRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(p => p.Id == comment.PostId, cancellationToken);

        if (post != null && post.CommentsCount > 0)
        {
            post.CommentsCount -= 1;
            _postRepository.Update(post);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
