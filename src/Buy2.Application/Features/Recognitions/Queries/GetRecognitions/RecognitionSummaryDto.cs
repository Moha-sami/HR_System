using System;

namespace Buy2.Application.Features.Recognitions.Queries.GetRecognitions;

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
    int AwardedPoints,
    DateTime? ScheduledFor,
    DateTime? PublishedAt,
    int LikesCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
