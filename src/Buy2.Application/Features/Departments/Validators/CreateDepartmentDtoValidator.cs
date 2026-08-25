using Buy2.Application.Features.Departments.DTOs;
using FluentValidation;

namespace Buy2.Application.Features.Departments.Validators;

public class CreateDepartmentDtoValidator : AbstractValidator<CreateDepartmentDto>
{
    public CreateDepartmentDtoValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Department name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Department name cannot be empty or whitespace.")
            .Must(name =>
            {
                var trimmed = name.Trim();
                return trimmed.Length >= 2 && trimmed.Length <= 100;
            }).WithMessage("Department name length must be between 2 and 100 characters.");

        RuleFor(x => x.Code)
            .Must(code => string.IsNullOrEmpty(code) || code.Trim().Length <= 20)
            .WithMessage("Department code cannot exceed 20 characters.");
    }
}
