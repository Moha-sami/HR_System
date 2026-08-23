using Buy2.Application.DTOs.Roles;
using FluentValidation;

namespace Buy2.Application.Validators;

/// <summary>
/// FluentValidation validator for CreateRoleDto requests.
/// </summary>
public class CreateRoleDtoValidator : AbstractValidator<CreateRoleDto>
{
    public CreateRoleDtoValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Role name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Role name cannot be empty or whitespace.")
            .Must(name =>
            {
                var trimmed = name.Trim();
                return trimmed.Length >= 2 && trimmed.Length <= 100;
            }).WithMessage("Role name length must be between 2 and 100 characters after trimming.");

        RuleFor(x => x.Description)
            .Must(desc => desc == null || desc.Length <= 500)
            .WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.Permissions)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Permissions list is required.")
            .NotEmpty().WithMessage("Permissions list cannot be empty.")
            .Must(permissions =>
            {
                if (permissions == null || permissions.Count == 0) return true;
                var trimmedModules = permissions
                    .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Module))
                    .Select(p => p.Module.Trim());
                return trimmedModules.Distinct(StringComparer.OrdinalIgnoreCase).Count() == permissions.Count;
            }).WithMessage("Permissions list must not contain duplicate module entries.");

        When(x => x.Permissions != null, () =>
        {
            RuleForEach(x => x.Permissions!)
                .SetValidator(new ModulePermissionDtoValidator());
        });
    }
}
