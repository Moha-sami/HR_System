using System.Collections.Generic;

namespace Buy2.Application.Features.News.Commands.ToggleReaction;

public record ReactionSummaryDto(
    string TargetType,
    int TargetId,
    int EmployeeId,
    string? UserReaction,
    string? ActiveReaction,
    bool IsActive,
    int TotalCount,
    Dictionary<string, int> ReactionBreakdown,
    string Message
);
