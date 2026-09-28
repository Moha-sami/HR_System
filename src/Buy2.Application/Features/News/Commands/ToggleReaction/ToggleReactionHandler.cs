using Buy2.Application.Common.Interfaces;
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

namespace Buy2.Application.Features.News.Commands.ToggleReaction;

public class ToggleReactionHandler : IRequestHandler<ToggleReactionCommand, ReactionSummaryDto>
{
    private readonly IRepository<Reaction> _reactionRepository;
    private readonly IRepository<Post> _postRepository;
    private readonly IRepository<Comment> _commentRepository;
    private readonly IRepository<Recognition> _recognitionRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ToggleReactionCommand> _validator;

    public ToggleReactionHandler(
        IRepository<Reaction> reactionRepository,
        IRepository<Post> postRepository,
        IRepository<Comment> commentRepository,
        IRepository<Recognition> recognitionRepository,
        IRepository<Employee> employeeRepository,
        IUnitOfWork unitOfWork,
        IValidator<ToggleReactionCommand>? validator = null)
    {
        _reactionRepository = reactionRepository;
        _postRepository = postRepository;
        _commentRepository = commentRepository;
        _recognitionRepository = recognitionRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _validator = validator ?? new ToggleReactionValidator();
    }

    public async Task<ReactionSummaryDto> Handle(ToggleReactionCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 1. Resolve employee
        int employeeId;
        if (request.CallerId.HasValue && request.CallerId.Value > 0)
        {
            var empExists = await _employeeRepository.Query(asNoTracking: true)
                .AnyAsync(e => e.Id == request.CallerId.Value, cancellationToken);
            if (!empExists)
            {
                throw new KeyNotFoundException($"Employee with ID {request.CallerId.Value} was not found.");
            }
            employeeId = request.CallerId.Value;
        }
        else
        {
            var defaultEmp = await _employeeRepository.Query(asNoTracking: true).FirstOrDefaultAsync(cancellationToken);
            if (defaultEmp == null)
            {
                throw new InvalidOperationException("No employee profile found to associate reaction with.");
            }
            employeeId = defaultEmp.Id;
        }

        // 2. Normalize target type and validate existence
        var trimmedType = request.TargetType.Trim();
        var normalizedTargetType = trimmedType.Equals("Post", StringComparison.OrdinalIgnoreCase) ? "Post"
            : trimmedType.Equals("Comment", StringComparison.OrdinalIgnoreCase) ? "Comment"
            : trimmedType.Equals("Recognition", StringComparison.OrdinalIgnoreCase) ? "Recognition"
            : trimmedType;

        Post? postTarget = null;
        if (normalizedTargetType == "Post")
        {
            postTarget = await _postRepository.Query(asNoTracking: false)
                .FirstOrDefaultAsync(p => p.Id == request.TargetId && p.PostType == "News" && !p.IsDeleted, cancellationToken);
            if (postTarget == null)
            {
                throw new KeyNotFoundException($"News post with ID {request.TargetId} was not found.");
            }
        }
        else if (normalizedTargetType == "Comment")
        {
            var commentTarget = await _commentRepository.Query(asNoTracking: true)
                .FirstOrDefaultAsync(c => c.Id == request.TargetId && !c.IsDeleted, cancellationToken);
            if (commentTarget == null)
            {
                throw new KeyNotFoundException($"Comment with ID {request.TargetId} was not found.");
            }
        }
        else if (normalizedTargetType == "Recognition")
        {
            var recognitionTarget = await _recognitionRepository.Query(asNoTracking: true)
                .FirstOrDefaultAsync(r => r.Id == request.TargetId && !r.IsDeleted, cancellationToken);
            if (recognitionTarget == null)
            {
                throw new KeyNotFoundException($"Recognition with ID {request.TargetId} was not found.");
            }
        }

        // 3. Retrieve existing reaction for caller and target
        var existingReaction = await _reactionRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId &&
                                      r.TargetType == normalizedTargetType &&
                                      r.TargetId == request.TargetId, cancellationToken);

        var currentReactionType = existingReaction?.ReactionType;
        var (newReaction, wasRemoved, wasSwitched, wasAdded) =
            SocialEngagementManager.CalculateReactionToggle(currentReactionType, request.ReactionType);

        string message;
        if (wasRemoved)
        {
            _reactionRepository.Delete(existingReaction!);
            message = $"Reaction '{currentReactionType}' removed.";
        }
        else if (wasSwitched)
        {
            existingReaction!.ReactionType = newReaction!;
            _reactionRepository.Update(existingReaction);
            message = $"Reaction switched from '{currentReactionType}' to '{newReaction}'.";
        }
        else
        {
            var reaction = new Reaction
            {
                TargetType = normalizedTargetType,
                TargetId = request.TargetId,
                EmployeeId = employeeId,
                ReactionType = newReaction!,
                PostId = normalizedTargetType == "Post" ? request.TargetId : null,
                CommentId = normalizedTargetType == "Comment" ? request.TargetId : null,
                RecognitionId = normalizedTargetType == "Recognition" ? request.TargetId : null
            };
            await _reactionRepository.AddAsync(reaction, cancellationToken);
            message = $"Reaction '{newReaction}' added.";
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Compute updated breakdown and tally
        var targetReactions = await _reactionRepository.Query(asNoTracking: true)
            .Where(r => r.TargetType == normalizedTargetType && r.TargetId == request.TargetId)
            .ToListAsync(cancellationToken);

        var breakdown = targetReactions
            .GroupBy(r => r.ReactionType)
            .ToDictionary(g => g.Key, g => g.Count());

        var totalCount = targetReactions.Count;
        var likesCount = targetReactions.Count(r => r.ReactionType == SocialEngagementManager.ReactionLike);

        if (postTarget != null)
        {
            postTarget.LikesCount = likesCount;
            _postRepository.Update(postTarget);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new ReactionSummaryDto(
            TargetType: normalizedTargetType,
            TargetId: request.TargetId,
            EmployeeId: employeeId,
            UserReaction: newReaction,
            ActiveReaction: newReaction,
            IsActive: newReaction != null,
            TotalCount: totalCount,
            ReactionBreakdown: breakdown,
            Message: message
        );
    }
}
