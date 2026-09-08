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
            .Must(BeValidSortDirection)
            .WithMessage("SortDir must be one of: asc, desc.");

        RuleFor(x => x.NameSort)
            .Must(BeValidSortDirection)
            .WithMessage("NameSort must be one of: asc, desc.");

        RuleFor(x => x.CreationSort)
            .Must(BeValidSortDirection)
            .WithMessage("CreationSort must be one of: asc, desc.");

        RuleFor(x => x.UpdatedSort)
            .Must(BeValidSortDirection)
            .WithMessage("UpdatedSort must be one of: asc, desc.");

        RuleFor(x => x.NumberOfAssignedSort)
            .Must(BeValidSortDirection)
            .WithMessage("NumberOfAssignedSort must be one of: asc, desc.");
    }

    private static bool BeValidSortDirection(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            || value.Equals("asc", StringComparison.OrdinalIgnoreCase)
            || value.Equals("desc", StringComparison.OrdinalIgnoreCase);
    }
}
