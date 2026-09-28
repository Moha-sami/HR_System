using System;
using System.Collections.Generic;
using System.Linq;

namespace Buy2.Application.Features.News.Common;

public static class SocialEngagementManager
{
    public const string ReactionLike = "Like";
    public const string ReactionDislike = "Dislike";
    public const string ReactionLaugh = "Laugh";
    public const string ReactionWow = "Wow";
    public const string ReactionHeart = "Heart";
    public const string ReactionAngry = "Angry";

    public const string TombstoneModeratedComment = "This comment has been removed by admin";
    public const string TombstoneDeletedComment = "[This comment has been deleted]";

    public static readonly HashSet<string> AllowedReactionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ReactionLike,
        ReactionDislike,
        ReactionLaugh,
        ReactionWow,
        ReactionHeart,
        ReactionAngry
    };

    public static bool IsValidReactionType(string? reactionType)
    {
        if (string.IsNullOrWhiteSpace(reactionType))
            return false;

        return AllowedReactionTypes.Contains(reactionType.Trim());
    }

    public static string NormalizeReactionType(string reactionType)
    {
        if (string.IsNullOrWhiteSpace(reactionType))
            throw new ArgumentException("Reaction type cannot be null or empty.", nameof(reactionType));

        var trimmed = reactionType.Trim();
        var match = AllowedReactionTypes.FirstOrDefault(r => string.Equals(r, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match == null)
        {
            throw new ArgumentException($"Invalid reaction type '{reactionType}'. Allowed values: Like, Dislike, Laugh, Wow, Heart, Angry.", nameof(reactionType));
        }

        return match;
    }

    public static string ResolveCommentDisplayContent(string content, bool isModerated, bool isDeleted)
    {
        if (isModerated)
        {
            return TombstoneModeratedComment;
        }

        if (isDeleted)
        {
            return TombstoneDeletedComment;
        }

        return content;
    }

    public static (string? NewReaction, bool WasRemoved, bool WasSwitched, bool WasAdded) CalculateReactionToggle(
        string? currentReaction,
        string requestedReaction)
    {
        var normalizedRequested = NormalizeReactionType(requestedReaction);

        if (string.IsNullOrWhiteSpace(currentReaction))
        {
            // Adding new reaction
            return (normalizedRequested, false, false, true);
        }

        if (string.Equals(currentReaction, normalizedRequested, StringComparison.OrdinalIgnoreCase))
        {
            // Toggled same reaction -> remove
            return (null, true, false, false);
        }

        // Toggled different reaction -> switch (mutually exclusive)
        return (normalizedRequested, false, true, false);
    }
}
