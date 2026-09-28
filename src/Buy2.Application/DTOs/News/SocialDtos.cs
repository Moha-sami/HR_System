using System;
using System.Collections.Generic;

namespace Buy2.Application.DTOs.News;

public record CommentReplyDto(
    int Id,
    int PostId,
    int? ParentCommentId,
    int AuthorId,
    string AuthorName,
    string? AuthorAvatar,
    string Content,
    bool IsModerated,
    string? ModerationReason,
    int LikesCount,
    Dictionary<string, int> ReactionBreakdown,
    string? UserReaction,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CommentThreadDto(
    int Id,
    int PostId,
    int? ParentCommentId,
    int AuthorId,
    string AuthorName,
    string? AuthorAvatar,
    string Content,
    bool IsModerated,
    string? ModerationReason,
    int LikesCount,
    Dictionary<string, int> ReactionBreakdown,
    string? UserReaction,
    List<CommentReplyDto> Replies,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateCommentDto(
    string Content,
    int? ParentCommentId = null
);

public record UpdateCommentDto(
    string Content
);

public record ToggleReactionRequestDto(
    string ReactionType
);

public record ReactionResultDto(
    string TargetType,
    int TargetId,
    int EmployeeId,
    string? ActiveReaction,
    bool IsActive,
    int TotalCount,
    Dictionary<string, int> Breakdown,
    string Message
);
