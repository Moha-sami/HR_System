using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.News;
using Buy2.Application.Features.News.Common;
using Buy2.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.News.Commands.UpdateComment;

public class UpdateCommentHandler : IRequestHandler<UpdateCommentCommand, CommentDto>
{
    private readonly IRepository<Comment> _commentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateCommentCommand> _validator;

    public UpdateCommentHandler(
        IRepository<Comment> commentRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateCommentCommand>? validator = null)
    {
        _commentRepository = commentRepository;
        _unitOfWork = unitOfWork;
        _validator = validator ?? new UpdateCommentValidator();
    }

    public async Task<CommentDto> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 1. Retrieve comment
        var comment = await _commentRepository.Query(asNoTracking: false)
            .Include(c => c.Author)
            .Include(c => c.Reactions)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken);

        if (comment == null || comment.IsDeleted)
        {
            throw new KeyNotFoundException($"Comment with ID {request.CommentId} was not found.");
        }

        // 2. Disallow editing if comment is marked as removed by administrator
        if (comment.IsModerated)
        {
            throw new InvalidOperationException("Cannot edit a comment that has been removed by an administrator.");
        }

        // 3. Enforce that only the original author can edit the comment
        if (!request.CallerId.HasValue || request.CallerId.Value != comment.AuthorId)
        {
            throw new UnauthorizedAccessException("Only the original author can edit this comment.");
        }

        // 4. Update content and record last-modified timestamp
        var now = DateTime.UtcNow;
        comment.Content = request.Content.Trim();
        comment.UpdatedAt = now;

        _commentRepository.Update(comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var authorName = comment.Author != null
            ? $"{comment.Author.FirstName} {comment.Author.LastName}".Trim()
            : "Unknown User";

        var likesCount = comment.Reactions?.Count(r => r.ReactionType == SocialEngagementManager.ReactionLike) ?? 0;

        return new CommentDto(
            Id: comment.Id,
            PostId: comment.PostId,
            ParentCommentId: comment.ParentCommentId,
            AuthorId: comment.AuthorId,
            AuthorName: string.IsNullOrWhiteSpace(authorName) ? "Unknown User" : authorName,
            AuthorAvatar: comment.Author?.ProfilePhotoUrl,
            Content: comment.Content,
            IsModerated: comment.IsModerated,
            ModerationReason: comment.ModerationReason,
            LikesCount: likesCount,
            CreatedAt: comment.CreatedAt,
            UpdatedAt: comment.UpdatedAt
        );
    }
}
