using Buy2.Application.DTOs.Roles;
using Buy2.Domain.Enums;
using Buy2.Domain.Permissions;
using FluentValidation;

namespace Buy2.Application.Validators;

/// <summary>
/// Validator for ModulePermissionDto enforcing valid module name, supported actions, and valid scope configuration.
/// </summary>
public class ModulePermissionDtoValidator : AbstractValidator<ModulePermissionDto>
{
    public ModulePermissionDtoValidator()
    {
        RuleFor(x => x.Module)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Module is required.")
            .Must(m => !string.IsNullOrWhiteSpace(m)).WithMessage("Module cannot be empty.")
            .Must(m => Enum.TryParse<PermissionModule>(m.Trim(), true, out var parsed) && Enum.IsDefined(typeof(PermissionModule), parsed))
            .WithMessage("Module must be a valid PermissionModule enum value name.");

        RuleFor(x => x.Actions)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Actions list is required.")
            .NotEmpty().WithMessage("Actions list cannot be empty.");

        RuleFor(x => x)
            .Must(dto =>
            {
                if (dto == null || dto.Actions == null || dto.Actions.Count == 0) return true;
                if (string.IsNullOrWhiteSpace(dto.Module)) return true;
                if (!Enum.TryParse<PermissionModule>(dto.Module.Trim(), true, out var parsedModule) || !Enum.IsDefined(typeof(PermissionModule), parsedModule))
                {
                    return true;
                }

                foreach (var action in dto.Actions)
                {
                    if (string.IsNullOrWhiteSpace(action) || !ModuleActionCatalog.IsActionSupported(parsedModule, action))
                    {
                        return false;
                    }
                }
                return true;
            })
            .WithMessage("Each action in Actions must be supported for the specified module.");

        RuleFor(x => x)
            .Must(dto =>
            {
                if (dto == null || dto.Scope == null || string.IsNullOrWhiteSpace(dto.Scope.ScopeType) || string.IsNullOrWhiteSpace(dto.Module)) return true;
                if (!Enum.TryParse<PermissionModule>(dto.Module.Trim(), true, out var parsedModule) || !Enum.IsDefined(typeof(PermissionModule), parsedModule)) return true;
                if (!Enum.TryParse<AccessScope>(dto.Scope.ScopeType.Trim(), true, out var parsedScope) || !Enum.IsDefined(typeof(AccessScope), parsedScope)) return true;

                return ModuleActionCatalog.IsScopeSupported(parsedModule, parsedScope);
            })
            .WithMessage(dto => $"ScopeType '{dto.Scope?.ScopeType}' is not supported for module '{dto.Module}'.");

        RuleFor(x => x.Scope)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Scope is required.");

        When(x => x.Scope != null, () =>
        {
            RuleFor(x => x.Scope!)
                .SetValidator(new PermissionScopeDtoValidator());
        });
    }
}
