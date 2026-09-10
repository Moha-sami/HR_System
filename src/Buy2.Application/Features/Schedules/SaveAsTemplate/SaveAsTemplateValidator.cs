using FluentValidation;

namespace Buy2.Application.Features.Schedules.SaveAsTemplate;

public class SaveAsTemplateValidator : AbstractValidator<SaveAsTemplateCommand>
{
    public const int MaxNameLength = 100;

    public SaveAsTemplateValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Name is required.")
            .Must(name => (name ?? string.Empty).Trim().Length <= MaxNameLength)
            .WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.SiteId)
            .GreaterThan(0)
            .WithMessage("SiteId must be greater than 0.");
    }
}
