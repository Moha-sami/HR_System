using Buy2.Application.Features.Qualifications.DTOs;
using FluentValidation;

namespace Buy2.Application.Features.Qualifications.Validators;

public class CreateQualificationDtoValidator : AbstractValidator<CreateQualificationDto>
{
    public CreateQualificationDtoValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Qualification name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Qualification name cannot be empty or whitespace.")
            .Must(name =>
            {
                var trimmed = name.Trim();
                return trimmed.Length >= 2 && trimmed.Length <= 150;
            }).WithMessage("Qualification name length must be between 2 and 150 characters.");

        RuleFor(x => x.Type)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Type is required.")
            .Must(cat => !string.IsNullOrWhiteSpace(cat)).WithMessage("Type cannot be empty or whitespace.")
            .Must(cat => cat.Trim().Length <= 50).WithMessage("Type length cannot exceed 50 characters.");
    }
}
