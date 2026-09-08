using Buy2.Application.DTOs.Rewards.DTOs;
using FluentValidation;

namespace Buy2.Application.Validators.Rewards;

public class RewardCreateDtoValidator : AbstractValidator<RewardCreateDto>
{
    public RewardCreateDtoValidator()
    {
        RuleFor(r => r.Name)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(r => r.CategoryId)
            .NotEmpty()
            .GreaterThan(0);
        RuleFor(r => r.Points)
            .NotEmpty()
            .GreaterThan(0);
        RuleFor(r => r.MonetaryValue)
            .GreaterThanOrEqualTo(0);
        RuleFor(r => r.HowToRedeem)
            .NotEmpty();
        RuleFor(r => r.TermsOfUse)
            .NotEmpty();
    }
}