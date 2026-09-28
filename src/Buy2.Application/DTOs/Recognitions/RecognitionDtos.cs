using System;
using System.Collections.Generic;

namespace Buy2.Application.DTOs.Recognitions;

public record RecognitionSummaryDto(
    int Id,
    string Title,
    string Narrative,
    string? Badge,
    string Status,
    int AuthorId,
    string AuthorName,
    int RecipientId,
    string RecipientName,
    string? RecipientAvatar,
    string? RecipientJobTitle,
    int AwardedPoints,
    DateTime? ScheduledFor,
    DateTime? PublishedAt,
    int LikesCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record RecognitionRecipientProfileDto(
    int Id,
    string FullName,
    string Email,
    string? ProfilePhotoUrl,
    string JobTitle,
    string Department
);

public record RecognitionDetailDto(
    int Id,
    string Title,
    string Narrative,
    string? Badge,
    string Status,
    int AuthorId,
    string AuthorName,
    string? AuthorAvatar,
    RecognitionRecipientProfileDto Recipient,
    int AwardedPoints,
    DateTime? ScheduledFor,
    DateTime? PublishedAt,
    int LikesCount,
    Dictionary<string, int> ReactionBreakdown,
    string? UserReaction,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateRecognitionDto(
    string Title,
    string Narrative,
    int RecipientId,
    int AwardedPoints = 0,
    string? Badge = null,
    string Status = "Published",
    DateTime? ScheduledFor = null
);

public record UpdateRecognitionDto(
    string Title,
    string Narrative,
    int RecipientId,
    int AwardedPoints = 0,
    string? Badge = null,
    string Status = "Published",
    DateTime? ScheduledFor = null
);

public record RecognitionResponseDto(
    int Id,
    string Title,
    string Status,
    int AwardedPoints,
    string Message
);
