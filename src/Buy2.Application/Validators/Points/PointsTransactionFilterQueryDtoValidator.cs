using Buy2.Application.DTOs.Points.DTOs;
using FluentValidation;

namespace Buy2.Application.Validators.Points;

public class PointsTransactionFilterQueryDtoValidator : AbstractValidator<PointsTransactionFilterQueryDto>
{
    private static readonly string[] ValidTransactionTypes = ["Add", "Deduct", "Reward", "Deduction", "Earned", "Redeemed"];
    private static readonly string[] ValidSortByFields = ["CreatedAt", "Date", "TransactionType", "Points", "Amount", "EmployeeName"];
    private static readonly string[] ValidSortDirections = ["Asc", "Desc"];

    public PointsTransactionFilterQueryDtoValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom.Value <= x.DateTo.Value)
            .WithMessage("DateFrom must be less than or equal to DateTo.");

        RuleFor(x => x.TransactionType)
            .Must(t => string.IsNullOrEmpty(t) || ValidTransactionTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("TransactionType must be one of: Reward, Deduction.");

        RuleFor(x => x.SortBy)
            .Must(s => string.IsNullOrEmpty(s) || ValidSortByFields.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortBy must be one of: TransactionType, Points, EmployeeName.");

        RuleFor(x => x.SortDir)
            .Must(s => string.IsNullOrEmpty(s) || ValidSortDirections.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortDir must be one of: Asc, Desc.");
    }
}