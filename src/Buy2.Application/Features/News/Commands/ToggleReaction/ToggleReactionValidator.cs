using Buy2.Application.Features.News.Common;
using FluentValidation;
using System;
using System.Collections.Generic;

namespace Buy2.Application.Features.News.Commands.ToggleReaction;

public class ToggleReactionValidator : AbstractValidator<ToggleReactionCommand>
{
    private static readonly HashSet<string> ValidTargetTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Post", "Comment", "Recognition"
    };

    public ToggleReactionValidator()
    {
        RuleFor(x => x.TargetType)
            .NotEmpty().WithMessage("Target type is required.")
            .Must(t => !string.IsNullOrWhiteSpace(t) && ValidTargetTypes.Contains(t.Trim()))
            .WithMessage("Target type must be Post, Comment, or Recognition.");

        RuleFor(x => x.TargetId)
            .GreaterThan(0).WithMessage("Target ID must be greater than 0.");

        RuleFor(x => x.ReactionType)
            .NotEmpty().WithMessage("Reaction type is required.")
            .Must(SocialEngagementManager.IsValidReactionType)
            .WithMessage("Invalid reaction type. Allowed values: Like, Dislike, Laugh, Wow, Heart, Angry.");
    }
}
