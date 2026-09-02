using Buy2.Application.DTOs.Points.DTOs;
using FluentValidation;

namespace Buy2.Application.Validators.Points;

public class CreateManualPointsTransactionDtoValidator 
    : AbstractValidator<CreateManualPointsTransactionDto>
{
    public CreateManualPointsTransactionDtoValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0)
            .WithMessage("EmployeeId must be greater than 0.");

        RuleFor(x => x.TransactionType)
            .NotEmpty()
            .WithMessage("TransactionType is required.")
            .Must(t =>
                t.Equals("Add", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("Deduct", StringComparison.OrdinalIgnoreCase))
            .WithMessage("TransactionType must be one of: Add, Deduct.");

        RuleFor(x => x.PointsValue)
            .InclusiveBetween(50m, 10000m)
            .WithMessage("PointsValue must be between 50 and 10,000.");

        RuleFor(x => x.Comments)
            .NotEmpty()
            .WithMessage("Comments is required.")
            .Length(3, 1000)
            .WithMessage("Comments must be between 3 and 1000 characters.");
    }
}
