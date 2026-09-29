using Buy2.Application.Common.Utilities;
using Buy2.Application.Features.Recognitions.Common;
using FluentValidation;
using System;

namespace Buy2.Application.Features.Recognitions.Commands.CreateRecognition;

public class CreateRecognitionValidator : AbstractValidator<CreateRecognitionCommand>
{
    public CreateRecognitionValidator()
    {
        RuleFor(x => x.RecipientId)
            .GreaterThan(0).WithMessage("Recipient employee ID must be greater than 0.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Recognition title is required.")
            .MaximumLength(200).WithMessage("Recognition title cannot exceed 200 characters.");

        RuleFor(x => x.Narrative)
            .NotEmpty().WithMessage("Recognition narrative/description is required.");

        RuleFor(x => x.AwardedPoints)
            .GreaterThanOrEqualTo(0).WithMessage("Awarded points cannot be negative.")
            .LessThanOrEqualTo(RecognitionLifecycleManager.MaxAwardedPoints)
            .WithMessage($"Awarded points cannot exceed {RecognitionLifecycleManager.MaxAwardedPoints} points per recognition.");

        RuleFor(x => x.Status)
            .Must(RecognitionLifecycleManager.IsValidStatus)
            .WithMessage("Invalid recognition status.");

        When(x => string.Equals(x.Status, RecognitionLifecycleManager.StatusScheduled, StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.ScheduledFor)
                .NotNull().WithMessage("Scheduled release timestamp is required for scheduled recognitions.")
                .GreaterThan(DateTime.UtcNow).WithMessage("Scheduled release timestamp must be in the future.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Badge), () =>
        {
            RuleFor(x => x.Badge)
                .Must(RecognitionLifecycleManager.IsValidBadge)
                .WithMessage("Invalid badge name.");
        });

        When(x => x.AttachmentFile != null, () =>
        {
            RuleFor(x => x.AttachmentFile!)
                .Must(f => f.Length <= NewsMediaValidator.MaxFileSizeBytes)
                .WithMessage("Attachment file size exceeds the 10 MB limit.");
        });
    }
}
