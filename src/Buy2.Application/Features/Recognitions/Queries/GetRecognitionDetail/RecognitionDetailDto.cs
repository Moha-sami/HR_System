using System;
using System.Collections.Generic;

namespace Buy2.Application.Features.Recognitions.Queries.GetRecognitionDetail;

public record RecognitionRecipientDetailDto(
    int Id,
    string FullName,
    string? Avatar,
    string? ProfilePhotoUrl,
    string JobTitle,
    string? Department,
    string ProfileUrl
);

public record RecognitionAuditDto(
    string CreatorName,
    DateTime CreatedAt,
    string? LastUpdaterName,
    DateTime? UpdatedAt
);

public record RecognitionDetailDto(
    int Id,
    string Title,
    string Narrative,
    int AwardedPoints,
    string? Badge,
    string Status,
    DateTime? ScheduledFor,
    DateTime? PublishedAt,
    string? AttachmentUrl,
    RecognitionRecipientDetailDto Recipient,
    RecognitionAuditDto Audit,
    int LikesCount,
    Dictionary<string, int> ReactionBreakdown,
    string? UserReaction,
    int AuthorId,
    string AuthorName,
    string? AuthorAvatar,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
