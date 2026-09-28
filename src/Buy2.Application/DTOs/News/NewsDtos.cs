using System;

namespace Buy2.Application.DTOs.News;

public record NewsFeedSummaryDto(
    int Id,
    string Title,
    string Summary,
    string Category,
    string Status,
    int AuthorId,
    string AuthorName,
    string? AuthorAvatar,
    string? MediaUrl,
    DateTime? ScheduledFor,
    DateTime? PublishedAt,
    int LikesCount,
    int CommentsCount,
    DateTime CreatedAt
);

public record NewsPostDetailDto(
    int Id,
    string Title,
    string Content,
    string Category,
    string Status,
    int AuthorId,
    string AuthorName,
    string? AuthorAvatar,
    string? MediaUrl,
    DateTime? ScheduledFor,
    DateTime? PublishedAt,
    int LikesCount,
    int CommentsCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateNewsPostDto(
    string Title,
    string Content,
    string Category,
    string Status = "Draft",
    DateTime? ScheduledFor = null
);

public record UpdateNewsPostDto(
    string Title,
    string Content,
    string Category,
    string Status,
    DateTime? ScheduledFor = null
);

public record NewsPostResponseDto(
    int Id,
    string Title,
    string Status,
    string Message
);
