using Buy2.Application.Features.ShiftTemplates.DTOs;
using FluentValidation;

namespace Buy2.Application.Features.ShiftTemplates.Validators;

public class ShiftTemplateFilterQueryDtoValidator : AbstractValidator<ShiftTemplateFilterQueryDto>
{
    public ShiftTemplateFilterQueryDtoValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.SortDir)
            .Must(s => string.IsNullOrWhiteSpace(s)
                || s.Equals("asc", StringComparison.OrdinalIgnoreCase)
                || s.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDir must be one of: asc, desc.");
    }
}
