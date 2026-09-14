using Buy2.Application.DTOs.Points.DTOs;
using FluentValidation;

namespace Buy2.Application.Validators.Points;

public class SaveAutomationSettingsDtoValidator : AbstractValidator<SaveAutomationSettingsDto>
{
    private static readonly string[] ValidAutomationPeriods = ["Daily", "Weekly", "BiWeekly", "Monthly"];

    public static bool IsValidAutomationPeriod(string? period) =>
        !string.IsNullOrWhiteSpace(period) &&
        ValidAutomationPeriods.Contains(period.Trim(), StringComparer.OrdinalIgnoreCase);

    public SaveAutomationSettingsDtoValidator()
    {
        RuleFor(x => x.AutomationPeriod)
            .NotEmpty()
            .WithMessage("AutomationPeriod is required.")
            .Must(p => SaveAutomationSettingsDtoValidator.IsValidAutomationPeriod(p))
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
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in settings)
        {
            if (category.Ranges == null) continue;
            foreach (var range in category.Ranges)
            {
                if (!string.IsNullOrWhiteSpace(range.TaskPriority))
                {
                    var key = $"{category.Category}|{category.SubCategory}|{range.TaskPriority.Trim()}";
                    if (!seen.Add(key))
                        return false;
                }
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
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("SubCategory is required.")
            .NotEmpty()
            .WithMessage("SubCategory is required.")
            .Must(s => !string.IsNullOrWhiteSpace(s!.Trim()))
            .WithMessage("SubCategory cannot be empty or whitespace.");

        RuleFor(x => x.IsEnabled)
            .NotNull()
            .WithMessage("IsEnabled is required and must be true or false.");

        RuleFor(x => x.Ranges)
            .NotNull()
            .WithMessage("Ranges are required.")
            .NotEmpty()
            .WithMessage("Ranges cannot be empty.")
            .When(x => x.IsEnabled == true);

        RuleFor(x => x.Ranges!)
            .Must(ValidateNoOverlappingRanges)
            .WithMessage("Ranges must not overlap.")
            .OverridePropertyName("Ranges")
            .When(x => x.IsEnabled == true && x.Ranges != null && x.Ranges.Any());

        RuleForEach(x => x.Ranges!)
            .SetValidator(new AutomationRangeDtoValidator())
            .When(x => x.IsEnabled == true && x.Ranges != null);
    }

    private static bool ValidateNoOverlappingRanges(List<AutomationRangeDto> ranges)
    {
        var valueRanges = ranges
            .Where(r => r.FromValue.HasValue && r.ToValue.HasValue)
            .OrderBy(r => r.FromValue!.Value)
            .ToList();

        for (int i = 0; i < valueRanges.Count - 1; i++)
        {
            var current = valueRanges[i];
            var next = valueRanges[i + 1];
            if (current.ToValue!.Value > next.FromValue!.Value)
                return false;
        }
        return true;
    }
}

public class AutomationRangeDtoValidator : AbstractValidator<AutomationRangeDto>
{
    private static readonly string[] ValidTaskPriorities = ["Low", "Medium", "High", "Urgent"];

    public AutomationRangeDtoValidator()
    {
        RuleFor(x => x.RangeType)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("RangeType is required.")
            .Must(BeKnownRangeType)
            .WithMessage("RangeType must be either 'Reward' or 'Deduction'.");

        RuleFor(x => x.PointsValue)
            .NotEqual(0)
            .WithMessage("PointsValue cannot be zero.");

        RuleFor(x => x)
            .Must(BeValidSign)
            .WithMessage("Range sign is invalid: Deduction ranges must have a negative PointsValue, Reward ranges must have a positive PointsValue.")
            .OverridePropertyName(nameof(AutomationRangeDto.PointsValue));

        RuleFor(x => x.FromValue)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("FromValue is required.")
            .InclusiveBetween(0, 100)
            .WithMessage("FromValue must be between 0 and 100.");

        RuleFor(x => x.ToValue)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("ToValue is required.")
            .InclusiveBetween(0, 100)
            .WithMessage("ToValue must be between 0 and 100.");

        RuleFor(x => x)
            .Must(x => !x.FromValue.HasValue || !x.ToValue.HasValue || x.FromValue.Value <= x.ToValue.Value)
            .WithMessage("FromValue must be less than or equal to ToValue.")
            .OverridePropertyName(nameof(AutomationRangeDto.FromValue));

        RuleFor(x => x.TaskPriority)
            .Must(p => string.IsNullOrWhiteSpace(p) || ValidTaskPriorities.Contains(p.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("TaskPriority must be one of: Low, Medium, High, Urgent.");
    }

    private static bool BeKnownRangeType(string rangeType)
    {
        var t = rangeType?.Trim();
        return t != null
            && (t.Equals("Reward", StringComparison.OrdinalIgnoreCase)
                || t.Equals("Deduction", StringComparison.OrdinalIgnoreCase));
    }

    private static bool BeValidSign(AutomationRangeDto range)
    {
        var t = range.RangeType?.Trim();
        if (t == null || range.PointsValue == 0)
            return true;
        if (t.Equals("Deduction", StringComparison.OrdinalIgnoreCase))
            return range.PointsValue < 0;
        if (t.Equals("Reward", StringComparison.OrdinalIgnoreCase))
            return range.PointsValue > 0;
        return true;
    }
}
