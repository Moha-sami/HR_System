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
            .Must(t => t.Equals("Reward", StringComparison.OrdinalIgnoreCase) || t.Equals("Deduction", StringComparison.OrdinalIgnoreCase))
            .WithMessage("TransactionType must be either 'Reward' or 'Deduction'.");

        RuleFor(x => x.PointsValue)
            .NotEqual(0m)
            .WithMessage("PointsValue cannot be zero.");

        RuleFor(x => x)
            .Must(ValidatePointsValueSign)
            .WithMessage("PointsValue sign must match TransactionType: Reward requires positive, Deduction requires negative.")
            .OverridePropertyName(nameof(CreateManualPointsTransactionDto.PointsValue));

        RuleFor(x => x)
            .Must(ValidatePointsValueRange)
            .WithMessage("PointsValue out of range. Reward: 50 to 10000. Deduction: -10000 to -50.")
            .OverridePropertyName(nameof(CreateManualPointsTransactionDto.PointsValue));

        RuleFor(x => x.Comments)
            .NotEmpty()
            .WithMessage("Comments is required.")
            .Length(3, 1000)
            .WithMessage("Comments must be between 3 and 1000 characters.");
    }

    private static bool ValidatePointsValueSign(CreateManualPointsTransactionDto dto)
    {
        if (string.IsNullOrEmpty(dto.TransactionType))
            return true;

        var isReward = dto.TransactionType.Equals("Reward", StringComparison.OrdinalIgnoreCase);
        var isDeduction = dto.TransactionType.Equals("Deduction", StringComparison.OrdinalIgnoreCase);

        if (isReward && dto.PointsValue > 0)
            return true;
        if (isDeduction && dto.PointsValue < 0)
            return true;

        return false;
    }

    private static bool ValidatePointsValueRange(CreateManualPointsTransactionDto dto)
    {
        if (string.IsNullOrEmpty(dto.TransactionType))
            return true;

        var isReward = dto.TransactionType.Equals("Reward", StringComparison.OrdinalIgnoreCase);
        var isDeduction = dto.TransactionType.Equals("Deduction", StringComparison.OrdinalIgnoreCase);

        if (isReward)
            return dto.PointsValue >= 50m && dto.PointsValue <= 10000m;
        if (isDeduction)
            return dto.PointsValue <= -50m && dto.PointsValue >= -10000m;

        return false;
    }
}