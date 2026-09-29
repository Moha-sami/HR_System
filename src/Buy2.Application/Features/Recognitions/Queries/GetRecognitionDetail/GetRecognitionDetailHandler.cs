using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Recognitions.Common;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buy2.Application.Features.Recognitions.Queries.GetRecognitionDetail;

public class GetRecognitionDetailHandler : IRequestHandler<GetRecognitionDetailQuery, RecognitionDetailDto>
{
    private readonly IRepository<Recognition> _recognitionRepository;
    private readonly IRepository<JobRole>? _jobRoleRepository;

    public GetRecognitionDetailHandler(
        IRepository<Recognition> recognitionRepository,
        IRepository<JobRole>? jobRoleRepository = null)
    {
        _recognitionRepository = recognitionRepository;
        _jobRoleRepository = jobRoleRepository;
    }

    public async Task<RecognitionDetailDto> Handle(GetRecognitionDetailQuery request, CancellationToken cancellationToken)
    {
        var recognition = await _recognitionRepository.Query(asNoTracking: true)
            .Include(r => r.Author)
            .Include(r => r.Recipient)
            .Include(r => r.Reactions)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (recognition == null || recognition.IsDeleted)
        {
            throw new KeyNotFoundException($"Recognition with ID {request.Id} was not found.");
        }

        var now = DateTime.UtcNow;
        var effectiveStatus = RecognitionLifecycleManager.ResolveEffectiveStatus(recognition.Status, recognition.ScheduledFor);

        bool isAuthor = request.CurrentUserId.HasValue && recognition.AuthorId == request.CurrentUserId.Value;
        bool isElevated = request.IsElevatedUser;

        if (!isElevated && !isAuthor)
        {
            bool isVisible = recognition.Status == RecognitionLifecycleManager.StatusPublished ||
                             (recognition.Status == RecognitionLifecycleManager.StatusScheduled && recognition.ScheduledFor.HasValue && recognition.ScheduledFor.Value <= now);

            if (!isVisible)
            {
                throw new KeyNotFoundException($"Recognition with ID {request.Id} was not found.");
            }
        }

        var recipient = recognition.Recipient;
        var recipientName = recipient != null ? $"{recipient.FirstName} {recipient.LastName}".Trim() : "Colleague";

        string? jobTitle = recipient?.JobRole?.Title;
        string? departmentName = recipient?.JobRole?.Department?.Name;

        if (string.IsNullOrWhiteSpace(jobTitle) && recipient != null && recipient.JobRoleId > 0 && _jobRoleRepository != null)
        {
            var jobRole = await _jobRoleRepository.Query(asNoTracking: true)
                .Include(j => j.Department)
                .FirstOrDefaultAsync(j => j.Id == recipient.JobRoleId, cancellationToken);

            if (jobRole != null)
            {
                jobTitle = jobRole.Title;
                departmentName = jobRole.Department?.Name;
            }
        }

        if (string.IsNullOrWhiteSpace(jobTitle))
        {
            jobTitle = !string.IsNullOrWhiteSpace(recipient?.JobType) ? recipient.JobType : "Employee";
        }

        var profileUrl = $"/api/v1/employees/{recognition.RecipientId}";

        var recipientDto = new RecognitionRecipientDetailDto(
            Id: recognition.RecipientId,
            FullName: recipientName,
            Avatar: recipient?.ProfilePhotoUrl,
            ProfilePhotoUrl: recipient?.ProfilePhotoUrl,
            JobTitle: jobTitle,
            Department: departmentName,
            ProfileUrl: profileUrl
        );

        var creatorName = recognition.Author != null
            ? $"{recognition.Author.FirstName} {recognition.Author.LastName}".Trim()
            : "Corporate HR";

        var lastUpdaterName = creatorName;

        var auditDto = new RecognitionAuditDto(
            CreatorName: creatorName,
            CreatedAt: recognition.CreatedAt,
            LastUpdaterName: lastUpdaterName,
            UpdatedAt: recognition.UpdatedAt
        );

        var likesCount = recognition.Reactions?.Count(react => react.ReactionType == "Like" || react.ReactionType == "like") ?? 0;

        var reactionBreakdown = recognition.Reactions?
            .GroupBy(react => react.ReactionType)
            .ToDictionary(g => g.Key, g => g.Count())
            ?? new Dictionary<string, int>();

        string? userReaction = null;
        if (request.CurrentUserId.HasValue && recognition.Reactions != null)
        {
            userReaction = recognition.Reactions
                .FirstOrDefault(react => react.EmployeeId == request.CurrentUserId.Value)?
                .ReactionType;
        }

        return new RecognitionDetailDto(
            Id: recognition.Id,
            Title: recognition.Title,
            Narrative: recognition.Narrative,
            AwardedPoints: recognition.AwardedPoints,
            Badge: recognition.Badge,
            Status: effectiveStatus,
            ScheduledFor: recognition.ScheduledFor,
            PublishedAt: recognition.PublishedAt,
            AttachmentUrl: recognition.AttachmentUrl,
            Recipient: recipientDto,
            Audit: auditDto,
            LikesCount: likesCount,
            ReactionBreakdown: reactionBreakdown,
            UserReaction: userReaction,
            AuthorId: recognition.AuthorId,
            AuthorName: creatorName,
            AuthorAvatar: recognition.Author?.ProfilePhotoUrl,
            CreatedAt: recognition.CreatedAt,
            UpdatedAt: recognition.UpdatedAt
        );
    }
}
