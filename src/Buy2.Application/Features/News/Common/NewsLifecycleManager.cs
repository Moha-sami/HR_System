using System;
using System.Collections.Generic;

namespace Buy2.Application.Features.News.Common;

public static class NewsLifecycleManager
{
    public const string StatusDraft = "Draft";
    public const string StatusScheduled = "Scheduled";
    public const string StatusPublished = "Published";
    public const string StatusArchived = "Archived";

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        StatusDraft, StatusScheduled, StatusPublished, StatusArchived
    };

    public static bool IsValidStatus(string status) =>
        !string.IsNullOrWhiteSpace(status) && ValidStatuses.Contains(status.Trim());

    public static (bool IsValid, string? ErrorMessage) ValidateTransition(string currentStatus, string targetStatus, DateTime? scheduledFor)
    {
        if (!IsValidStatus(targetStatus))
        {
            return (false, $"Invalid target status '{targetStatus}'.");
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
}
