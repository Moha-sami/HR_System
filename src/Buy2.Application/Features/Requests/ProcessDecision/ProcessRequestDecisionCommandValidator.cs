using FluentValidation;
using System;

namespace Buy2.Application.Features.Requests.ProcessDecision;

public class ProcessRequestDecisionCommandValidator : AbstractValidator<ProcessRequestDecisionCommand>
{
    public ProcessRequestDecisionCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .GreaterThan(0)
            .WithMessage("Request ID must be greater than 0.");

        RuleFor(x => x.Tier)
            .Must(t => !string.IsNullOrWhiteSpace(t) &&
                       (string.Equals(t.Trim(), "Manager", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(t.Trim(), "HR", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Tier must be either 'Manager' or 'HR'.");

        RuleFor(x => x.Decision)
            .Must(d => !string.IsNullOrWhiteSpace(d) &&
                       (string.Equals(d.Trim(), "Approved", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(d.Trim(), "Rejected", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Decision must be either 'Approved' or 'Rejected'.");

        When(x => !string.IsNullOrWhiteSpace(x.Decision) &&
                  string.Equals(x.Decision.Trim(), "Rejected", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.Comment)
                .NotEmpty()
                .WithMessage("Comment is required when rejecting a request.")
                .MaximumLength(1000)
                .WithMessage("Comment cannot exceed 1000 characters.");

            RuleFor(x => x.RejectionReason)
                .NotEmpty()
                .WithMessage("Rejection reason is required when rejecting a request.")
                .MaximumLength(500)
                .WithMessage("Rejection reason cannot exceed 500 characters.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Decision) &&
                  string.Equals(x.Decision.Trim(), "Approved", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.Comment)
                .MaximumLength(1000)
                .WithMessage("Comment cannot exceed 1000 characters.");
        });
    }
}
