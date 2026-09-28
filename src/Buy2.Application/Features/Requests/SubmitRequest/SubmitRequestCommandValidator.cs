using FluentValidation;
using System;

namespace Buy2.Application.Features.Requests.SubmitRequest;

public class SubmitRequestCommandValidator : AbstractValidator<SubmitRequestCommand>
{
    public SubmitRequestCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0)
            .WithMessage("Employee ID must be greater than 0.");

        RuleFor(x => x.RequestTypeId)
            .GreaterThan(0)
            .WithMessage("Request type ID must be greater than 0.");

        When(x => x.StartDate.HasValue && x.EndDate.HasValue, () =>
        {
            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("End date must be greater than or equal to start date.");
        });

        RuleFor(x => x.Reason)
            .MaximumLength(1000)
            .WithMessage("Reason cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
