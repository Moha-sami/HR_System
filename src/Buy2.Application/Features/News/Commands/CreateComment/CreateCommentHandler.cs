using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.News;
using Buy2.Application.Features.News.Common;
using Buy2.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.News.Commands.CreateComment;

public class CreateCommentHandler : IRequestHandler<CreateCommentCommand, CommentDto>
{
    private readonly IRepository<Post> _postRepository;
    private readonly IRepository<Comment> _commentRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCommentCommand> _validator;

    public CreateCommentHandler(
        IRepository<Post> postRepository,
        IRepository<Comment> commentRepository,
        IRepository<Employee> employeeRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateCommentCommand>? validator = null)
    {
        _postRepository = postRepository;
        _commentRepository = commentRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _validator = validator ?? new CreateCommentValidator();
    }

    public async Task<CommentDto> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 1. Verify post existence and active state
        var post = await _postRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(p => p.Id == request.PostId && p.PostType == "News", cancellationToken);

        if (post == null || post.IsDeleted)
        {
            throw new KeyNotFoundException($"News post with ID {request.PostId} was not found.");
        }

        var isPublished = string.Equals(post.Status, NewsLifecycleManager.StatusPublished, StringComparison.OrdinalIgnoreCase);
        var isScheduledReleased = string.Equals(post.Status, NewsLifecycleManager.StatusScheduled, StringComparison.OrdinalIgnoreCase)
            && post.ScheduledFor.HasValue && post.ScheduledFor.Value <= DateTime.UtcNow;

        if (!isPublished && !isScheduledReleased)
        {
            throw new InvalidOperationException("Cannot add comment to an unpublished or inactive news post.");
        }

        // 2. If replying, verify parent comment belongs to the target post
        if (request.ParentCommentId.HasValue)
        {
            var parentComment = await _commentRepository.Query(asNoTracking: true)
                .FirstOrDefaultAsync(c => c.Id == request.ParentCommentId.Value, cancellationToken);

            if (parentComment == null || parentComment.IsDeleted)
            {
                throw new KeyNotFoundException($"Parent comment with ID {request.ParentCommentId.Value} was not found.");
            }

            if (parentComment.PostId != request.PostId)
            {
                throw new InvalidOperationException($"Parent comment with ID {request.ParentCommentId.Value} does not belong to post {request.PostId}.");
            }
        }

        // 3. Resolve author
        Employee? author = null;
        if (request.AuthorId.HasValue && request.AuthorId.Value > 0)
        {
            author = await _employeeRepository.Query(asNoTracking: true)
                .FirstOrDefaultAsync(e => e.Id == request.AuthorId.Value, cancellationToken);

            if (author == null)
            {
                throw new KeyNotFoundException($"Author with ID {request.AuthorId.Value} was not found.");
            }
        }
        else
        {
            author = await _employeeRepository.Query(asNoTracking: true)
                .FirstOrDefaultAsync(cancellationToken);

            if (author == null)
            {
                throw new InvalidOperationException("No employee profile found to assign as comment author.");
            }
        }

        // 4. Create and stage comment entity
        var now = DateTime.UtcNow;
        var comment = new Comment
        {
            PostId = post.Id,
            AuthorId = author.Id,
            ParentCommentId = request.ParentCommentId,
            Content = request.Content.Trim(),
            CreatedAt = now,
            IsDeleted = false,
            IsModerated = false
        };

        await _commentRepository.AddAsync(comment, cancellationToken);

        // 5. Increment parent post comments counter
        post.CommentsCount += 1;
        _postRepository.Update(post);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var authorName = $"{author.FirstName} {author.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(authorName))
        {
            authorName = "Unknown User";
        }

        return new CommentDto(
            Id: comment.Id,
            PostId: comment.PostId,
            ParentCommentId: comment.ParentCommentId,
            AuthorId: author.Id,
            AuthorName: authorName,
            AuthorAvatar: author.ProfilePhotoUrl,
            Content: comment.Content,
            IsModerated: comment.IsModerated,
            ModerationReason: comment.ModerationReason,
            LikesCount: 0,
            CreatedAt: comment.CreatedAt,
            UpdatedAt: comment.UpdatedAt
        );
    }
}
