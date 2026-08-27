using System;
using System.Linq;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Application.Features.Jobs.DTOs;
using FluentValidation;

namespace Buy2.Application.Features.Jobs.Validators;

public class CreateJobDtoValidator : AbstractValidator<CreateJobDto>
{
    private static readonly string[] ValidSeniorityLevels = { "Junior", "MidLevel", "Senior", "Lead", "Manager", "Director" };
    private static readonly string[] ValidWorkModels = { "OnSite", "Remote", "Hybrid" };

    public CreateJobDtoValidator()
    {
        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Title is required.")
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithMessage("Title cannot be empty or whitespace.")
            .Must(title =>
            {
                var trimmed = title.Trim();
                return trimmed.Length >= 2 && trimmed.Length <= 100;
            }).WithMessage("Title length must be between 2 and 100 characters.");

        RuleFor(x => x)
            .Must(x => (x.DepartmentId.HasValue && x.DepartmentId.Value > 0) || !string.IsNullOrWhiteSpace(x.NewDepartmentName))
            .WithMessage("Either DepartmentId or NewDepartmentName must be specified.");

        RuleFor(x => x.SeniorityLevel)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("SeniorityLevel is required.")
            .Must(s => !string.IsNullOrWhiteSpace(s) && ValidSeniorityLevels.Contains(s.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("SeniorityLevel must be a valid value (Junior, MidLevel, Senior, Lead, Manager, Director).");

        RuleFor(x => x.WorkModel)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("WorkModel is required.")
            .Must(w => !string.IsNullOrWhiteSpace(w) && ValidWorkModels.Contains(w.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("WorkModel must be a valid value (OnSite, Remote, Hybrid).");

        RuleFor(x => x.ExperienceYearsMin)
            .GreaterThanOrEqualTo(0).WithMessage("ExperienceYearsMin must be greater than or equal to 0.");
    }
}
