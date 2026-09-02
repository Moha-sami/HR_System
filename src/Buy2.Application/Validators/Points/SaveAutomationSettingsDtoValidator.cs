using Buy2.Application.DTOs.Points.DTOs;
using FluentValidation;

namespace Buy2.Application.Validators.Points;

public class SaveAutomationSettingsDtoValidator : AbstractValidator<SaveAutomationSettingsDto>
{
    private static readonly string[] ValidAutomationPeriods = ["Daily", "Weekly", "BiWeekly", "Monthly"];

    public SaveAutomationSettingsDtoValidator()
    {
        RuleFor(x => x.AutomationPeriod)
            .NotEmpty()
            .WithMessage("AutomationPeriod is required.")
            .Must(p => ValidAutomationPeriods.Contains(p, StringComparer.OrdinalIgnoreCase))
            .WithMessage("AutomationPeriod must be one of: Daily, Weekly, BiWeekly, Monthly.");

        RuleFor(x => x.Settings)
            .NotNull()
            .WithMessage("Settings are required.")
            .NotEmpty()
            .WithMessage("Settings cannot be empty.");

        RuleFor(x => x.Settings!)
            .Must(ValidateNoDuplicateTaskPriority)
            .WithMessage("Duplicate TaskPriority found for the same Category and SubCategory combination.")
            .OverridePropertyName(nameof(SaveAutomationSettingsDto.Settings));

        RuleForEach(x => x.Settings!)
            .SetValidator(new AutomationSettingCategoryDtoValidator());
    }

    private static bool ValidateNoDuplicateTaskPriority(List<AutomationSettingCategoryDto> settings)
    {
        var seen = new HashSet<string>();
        foreach (var category in settings)
        {
            foreach (var range in category.Ranges)
            {
                var key = $"{category.Category}|{category.SubCategory}|{range.TaskPriority}";
                if (!seen.Add(key))
                    return false;
            }
        }
        return true;
    }
}

public class AutomationSettingCategoryDtoValidator : AbstractValidator<AutomationSettingCategoryDto>
{
    public AutomationSettingCategoryDtoValidator()
    {
        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Category is required.");

        RuleFor(x => x.SubCategory)
            .NotEmpty()
            .WithMessage("SubCategory is required.");

        RuleFor(x => x.Ranges)
            .NotNull()
            .WithMessage("Ranges are required.")
            .NotEmpty()
            .WithMessage("Ranges cannot be empty.");

        RuleFor(x => x.Ranges!)
            .Must(ValidateNoOverlappingRanges)
            .WithMessage("Ranges must not overlap or touch boundaries.")
            .OverridePropertyName("Ranges");

        RuleForEach(x => x.Ranges!)
            .SetValidator(new AutomationRangeDtoValidator());
    }

    private static bool ValidateNoOverlappingRanges(List<AutomationRangeDto> ranges)
    {
        var sorted = ranges.OrderBy(r => r.FromValue).ToList();
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            var current = sorted[i];
            var next = sorted[i + 1];
            if (current.ToValue >= next.FromValue)
                return false;
        }
        return true;
    }
}

public class AutomationRangeDtoValidator : AbstractValidator<AutomationRangeDto>
{
    public AutomationRangeDtoValidator()
    {
        RuleFor(x => x.RangeType)
            .NotEmpty()
            .WithMessage("RangeType is required.");

        RuleFor(x => x.FromValue)
            .LessThan(x => x.ToValue)
            .WithMessage("FromValue must be less than ToValue.");

        RuleFor(x => x.TaskPriority)
            .GreaterThan(0)
            .WithMessage("TaskPriority must be greater than 0.");

        RuleFor(x => x.PointsValue)
            .NotEqual(0)
            .WithMessage("PointsValue cannot be zero.");
    }
}