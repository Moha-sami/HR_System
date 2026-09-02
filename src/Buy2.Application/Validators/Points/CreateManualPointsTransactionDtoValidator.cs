using Buy2.Application.DTOs.Points.DTOs;
using FluentValidation;

namespace Buy2.Application.Validators.Points;

public class CreateManualPointsTransactionDtoValidator : AbstractValidator<CreateManualPointsTransactionDto>
{
    public CreateManualPointsTransactionDtoValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0)
            .WithMessage("EmployeeId must be greater than 0.");

        RuleFor(x => x.TransactionType)
            .NotEmpty()
            .WithMessage("TransactionType is required.")
            .Must(t => t.Equals("Add", StringComparison.OrdinalIgnoreCase) ||
                       t.Equals("Deduct", StringComparison.OrdinalIgnoreCase) ||
                       t.Equals("Reward", StringComparison.OrdinalIgnoreCase) ||
                       t.Equals("Deduction", StringComparison.OrdinalIgnoreCase))
            .WithMessage("TransactionType must be one of: Add, Deduct, Reward, Deduction.");

        RuleFor(x => x.PointsValue)
            .NotEqual(0m)
            .WithMessage("PointsValue cannot be zero.")
            .Must(v => Math.Abs(v) >= 1m && Math.Abs(v) <= 100000m)
            .WithMessage("PointsValue magnitude must be between 1 and 100,000.");

        RuleFor(x => x.Comments)
            .NotEmpty()
            .WithMessage("Comments is required.")
            .Length(3, 1000)
            .WithMessage("Comments must be between 3 and 1000 characters.");
    }
}