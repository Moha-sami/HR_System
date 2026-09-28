using System;
using System.Collections.Generic;

namespace Buy2.Application.Features.Recognitions.Common;

public static class RecognitionLifecycleManager
{
    public const string StatusDraft = "Draft";
    public const string StatusScheduled = "Scheduled";
    public const string StatusPublished = "Published";
    public const string StatusArchived = "Archived";

    public const int MaxAwardedPoints = 5000;
    public const int MinAwardedPoints = 0;

    public const string BadgeExcellence = "Excellence";
    public const string BadgeTeamPlayer = "TeamPlayer";
    public const string BadgeInnovator = "Innovator";
    public const string BadgeLeadership = "Leadership";
    public const string BadgeCustomerChampion = "CustomerChampion";
    public const string BadgeProblemSolver = "ProblemSolver";

    public static readonly HashSet<string> AllowedBadges = new(StringComparer.OrdinalIgnoreCase)
    {
        BadgeExcellence,
        BadgeTeamPlayer,
        BadgeInnovator,
        BadgeLeadership,
        BadgeCustomerChampion,
        BadgeProblemSolver
    };

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        StatusDraft, StatusScheduled, StatusPublished, StatusArchived
    };

    public static bool IsValidStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && ValidStatuses.Contains(status.Trim());

    public static bool IsValidBadge(string? badge) =>
        string.IsNullOrWhiteSpace(badge) || AllowedBadges.Contains(badge.Trim());

    public static (bool IsValid, string? ErrorMessage) ValidateTransition(string currentStatus, string targetStatus, DateTime? scheduledFor)
    {
        if (!IsValidStatus(targetStatus))
        {
            return (false, $"Invalid recognition target status '{targetStatus}'.");
        }

        if (string.Equals(targetStatus, StatusScheduled, StringComparison.OrdinalIgnoreCase))
        {
            if (!scheduledFor.HasValue)
            {
                return (false, "Scheduled status requires a future release timestamp.");
            }

            if (scheduledFor.Value <= DateTime.UtcNow)
            {
                return (false, "Scheduled release timestamp must be in the future.");
            }
        }

        return (true, null);
    }

    public static string ResolveEffectiveStatus(string currentStatus, DateTime? scheduledFor)
    {
        if (string.Equals(currentStatus, StatusScheduled, StringComparison.OrdinalIgnoreCase))
        {
            if (scheduledFor.HasValue && scheduledFor.Value <= DateTime.UtcNow)
            {
                return StatusPublished;
            }
        }

        return currentStatus;
    }

    public static (bool IsValid, string? ErrorMessage) ValidatePointsGrant(int points, int authorId, int recipientId)
    {
        if (authorId > 0 && authorId == recipientId)
        {
            return (false, "Employees cannot grant recognition reward points to themselves.");
        }

        if (points < MinAwardedPoints)
        {
            return (false, $"Awarded points cannot be negative.");
        }

        if (points > MaxAwardedPoints)
        {
            return (false, $"Awarded points cannot exceed maximum limit of {MaxAwardedPoints} points per recognition.");
        }

        return (true, null);
    }
}
