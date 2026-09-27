using FluentValidation;
using System;

namespace Buy2.Application.Features.Requests.UpdateRequestType;

public class UpdateRequestTypeCommandValidator : AbstractValidator<UpdateRequestTypeCommand>
{
    public UpdateRequestTypeCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Request type ID must be greater than 0.");

        RuleFor(x => x.Dto)
            .NotNull()
            .WithMessage("Request type payload cannot be null.");

        When(x => x.Dto != null, () =>
        {
            RuleFor(x => x.Dto.Name)
                .NotEmpty()
                .WithMessage("Request type name is required.")
                .MaximumLength(100)
                .WithMessage("Request type name cannot exceed 100 characters.");

            RuleFor(x => x.Dto.Category)
                .NotEmpty()
                .WithMessage("Request type category is required.")
                .MaximumLength(50)
                .WithMessage("Request type category cannot exceed 50 characters.");

            RuleFor(x => x.Dto.Hint)
                .MaximumLength(250)
                .WithMessage("Hint cannot exceed 250 characters.")
                .When(x => !string.IsNullOrEmpty(x.Dto.Hint));

            When(x => !string.IsNullOrWhiteSpace(x.Dto.Category) &&
                      string.Equals(x.Dto.Category.Trim(), "Leave", StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(x => x.Dto.LeaveType)
                    .Must(lt => string.IsNullOrEmpty(lt) || 
                                string.Equals(lt.Trim(), "Full", StringComparison.OrdinalIgnoreCase) || 
                                string.Equals(lt.Trim(), "Partial", StringComparison.OrdinalIgnoreCase))
                    .WithMessage("LeaveType must be either 'Full' or 'Partial'.");

                RuleFor(x => x.Dto.LeavePay)
                    .Must(lp => string.IsNullOrEmpty(lp) || 
                                string.Equals(lp.Trim(), "Paid", StringComparison.OrdinalIgnoreCase) || 
                                string.Equals(lp.Trim(), "Unpaid", StringComparison.OrdinalIgnoreCase))
                    .WithMessage("LeavePay must be either 'Paid' or 'Unpaid'.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.Dto.Category) &&
                      !string.Equals(x.Dto.Category.Trim(), "Leave", StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(x => x.Dto.LeaveType)
                    .Empty()
                    .WithMessage("LeaveType and LeavePay can only be configured for Leave requests.");

                RuleFor(x => x.Dto.LeavePay)
                    .Empty()
                    .WithMessage("LeaveType and LeavePay can only be configured for Leave requests.");
            });
        });
    }
}
