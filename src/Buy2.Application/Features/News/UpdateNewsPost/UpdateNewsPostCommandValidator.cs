using Buy2.Application.Common.Utilities;
using Buy2.Application.Features.News.Common;
using FluentValidation;
using System;

namespace Buy2.Application.Features.News.UpdateNewsPost;

public class UpdateNewsPostCommandValidator : AbstractValidator<UpdateNewsPostCommand>
{
    public UpdateNewsPostCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Valid post identifier is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(50).WithMessage("Category cannot exceed 50 characters.");

        RuleFor(x => x.Status)
            .Must(NewsLifecycleManager.IsValidStatus)
            .WithMessage("Invalid news post status.");

        When(x => string.Equals(x.Status, NewsLifecycleManager.StatusScheduled, StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.ScheduledFor)
                .NotNull().WithMessage("Scheduled release timestamp is required for scheduled posts.");
        });

        When(x => x.MediaFile != null, () =>
        {
            RuleFor(x => x.MediaFile!)
                .Must(f => f.Length <= NewsMediaValidator.MaxFileSizeBytes)
                .WithMessage("Media file size exceeds the 10 MB limit.")
                .Must(f => NewsMediaValidator.ValidateMedia(f.Length, f.FileName, f.ContentType).IsValid)
                .WithMessage((cmd, f) => NewsMediaValidator.ValidateMedia(f.Length, f.FileName, f.ContentType).ErrorMessage ?? "Invalid media file.");
        });
    }
}
