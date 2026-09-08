using Buy2.Application.DTOs.Rewards.DTOs;
using FluentValidation;

namespace Buy2.Application.Validators.Rewards;
public class RewardFilterDtoValidator : AbstractValidator<RewardFilterDto>
{
    public RewardFilterDtoValidator()
    {
        RuleFor(r => r.Page)
            .GreaterThan(0);
        RuleFor(r => r.PageSize)
            .LessThanOrEqualTo(10);
        RuleFor(r => r.ToDate)
            .GreaterThanOrEqualTo(r => r.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
    }
}