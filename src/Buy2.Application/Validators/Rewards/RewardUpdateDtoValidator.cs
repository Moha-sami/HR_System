using Buy2.Application.DTOs.Rewards.DTOs;
using FluentValidation;

namespace Buy2.Application.Validators.Rewards;
public class RewardUpdateDtoValidator : AbstractValidator<RewardUpdateDto>
{
    public RewardUpdateDtoValidator()
    {
        RuleFor(r => r.Name)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(r => r.CategoryId)
            .NotEmpty()
            .GreaterThan(0);
        RuleFor(r => r.Points)
            .GreaterThan(0);
        RuleFor(r => r.MonetaryValue)
            .GreaterThanOrEqualTo(0);
        RuleFor(r => r.HowToRedeem)
            .NotEmpty();
        RuleFor(r => r.TermsOfUse)
            .NotEmpty();
    }
}