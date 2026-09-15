using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Application.Validators.Points;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;

namespace Buy2.Application.Features.Points.Automation.SaveAutomationSettings;

public class SaveAutomationSettingsCommandHandler : IRequestHandler<SaveAutomationSettingsCommand, SaveAutomationSettingsResult>
{
    private readonly IRepository<PointsAutomationSetting> _automationSettingRepository;
    private readonly IRepository<PointsAutomationRange> _automationRangeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveAutomationSettingsCommandHandler(
        IRepository<PointsAutomationSetting> automationSettingRepository,
        IRepository<PointsAutomationRange> automationRangeRepository,
        IUnitOfWork unitOfWork)
    {
        _automationSettingRepository = automationSettingRepository;
        _automationRangeRepository = automationRangeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SaveAutomationSettingsResult> Handle(SaveAutomationSettingsCommand request, CancellationToken cancellationToken)
    {
        // Normalize first (defense-in-depth auto-fix): trim RangeType, fix sign
        // (Reward => +|v|, Deduction => -|v|), zero-out ranges of disabled settings.
        // The validator then acts as a second net for zero/unknown/garbage on enabled rows.
        var dto = NormalizeRequest(request.Request);

        var validation = await new SaveAutomationSettingsDtoValidator().ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return new SaveAutomationSettingsResult(
                IsSuccess: false,
                ErrorMessage: string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var normalized = new List<(AutomationCategory Category, string SubCategory, AutomationPeriod Period, bool IsEnabled, List<RangeInput> Ranges)>();

        if (!Enum.TryParse<AutomationPeriod>(dto.AutomationPeriod?.Trim(), ignoreCase: true, out var globalPeriod))
        {
            return new SaveAutomationSettingsResult(
                IsSuccess: false,
                ErrorMessage: "AutomationPeriod must be one of: Daily, Weekly, BiWeekly, Monthly.");
        }

        foreach (var setting in dto.Settings!)
        {
            if (!TryParseCategory(setting.Category, out var category))
            {
                return new SaveAutomationSettingsResult(
                    IsSuccess: false,
                    ErrorMessage: $"Unknown category '{setting.Category}'. Must be one of: Performance, Tasks, TimeAndAttendance.");
            }

            var subCategory = setting.SubCategory?.Trim();
            if (string.IsNullOrWhiteSpace(subCategory))
            {
                return new SaveAutomationSettingsResult(
                    IsSuccess: false,
                    ErrorMessage: "SubCategory is required.");
            }

            if (!setting.IsEnabled.HasValue)
            {
                return new SaveAutomationSettingsResult(
                    IsSuccess: false,
                    ErrorMessage: "IsEnabled is required and must be true or false.");
            }

            if (!setting.IsEnabled.Value)
            {
                normalized.Add((category, subCategory, globalPeriod, false, new List<RangeInput>()));
                continue;
            }

            if (setting.Ranges == null || setting.Ranges.Count == 0)
            {
                return new SaveAutomationSettingsResult(
                    IsSuccess: false,
                    ErrorMessage: "Ranges cannot be empty.");
            }

            foreach (var range in setting.Ranges!)
            {
                if (!range.FromValue.HasValue || !range.ToValue.HasValue)
                {
                    return new SaveAutomationSettingsResult(
                        IsSuccess: false,
                        ErrorMessage: "FromValue and ToValue are required.");
                }

                if (range.FromValue.Value < 0 || range.FromValue.Value > 100
                    || range.ToValue.Value < 0 || range.ToValue.Value > 100)
                {
                    return new SaveAutomationSettingsResult(
                        IsSuccess: false,
                        ErrorMessage: "FromValue and ToValue must be between 0 and 100.");
                }
            }

            normalized.Add((category, subCategory, globalPeriod, setting.IsEnabled.Value,
                setting.Ranges!.Select(r => new RangeInput(
                    r.RangeType.Trim(),
                    r.FromValue,
                    r.ToValue,
                    string.IsNullOrWhiteSpace(r.TaskPriority) ? null : r.TaskPriority.Trim(),
                    NormalizePointsValue(r.RangeType, r.PointsValue))).ToList()));
        }

        var duplicates = normalized
            .GroupBy(s => (s.Category, SubCategory: s.SubCategory.ToUpperInvariant()))
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.Category}:{g.First().SubCategory}")
            .ToList();

        if (duplicates.Any())
        {
            return new SaveAutomationSettingsResult(
                IsSuccess: false,
                ErrorMessage: $"Duplicate settings for: {string.Join(", ", duplicates)}.");
        }

        var deadlineWithoutPriority = normalized
            .Where(s => s.IsEnabled
                && s.Category == AutomationCategory.Tasks
                && s.SubCategory.Equals("Deadline", StringComparison.OrdinalIgnoreCase)
                && s.Ranges.All(r => string.IsNullOrWhiteSpace(r.TaskPriority)))
            .Select(s => $"{s.Category}:{s.SubCategory}")
            .ToList();

        if (deadlineWithoutPriority.Any())
        {
            return new SaveAutomationSettingsResult(
                IsSuccess: false,
                ErrorMessage: $"Deadline automation setting requires TaskPriority to be configured for at least one range: {string.Join(", ", deadlineWithoutPriority)}.");
        }

        var filterCategories = normalized.Select(n => n.Category).ToList();
        var filterSubCategories = normalized.Select(n => n.SubCategory.ToUpper()).ToList();
        var existingSpec = new Specification<PointsAutomationSetting>()
            .Include(nameof(PointsAutomationSetting.Ranges))
            .AsTracked()
            .Where(s => filterCategories.Contains(s.Category)
                && filterSubCategories.Contains(s.SubCategory.ToUpper()));
        var existingSettings = await _automationSettingRepository.ListAsync(existingSpec, cancellationToken);

        var newSettingsToInsert = new List<PointsAutomationSetting>();
        var newRangesToInsert = new List<PointsAutomationRange>();

        foreach (var input in normalized)
        {
            var existing = existingSettings.FirstOrDefault(s =>
                s.Category == input.Category
                && s.SubCategory.Equals(input.SubCategory, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                var created = new PointsAutomationSetting
                {
                    Category = input.Category,
                    SubCategory = input.SubCategory,
                    AutomationPeriod = input.Period,
                    IsEnabled = input.IsEnabled,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Ranges = input.Ranges.Select(r => new PointsAutomationRange
                    {
                        RangeType = r.RangeType,
                        FromValue = r.FromValue,
                        ToValue = r.ToValue,
                        TaskPriority = r.TaskPriority,
                        PointsValue = r.PointsValue
                    }).ToList()
                };

                newSettingsToInsert.Add(created);
            }
            else
            {
                existing.AutomationPeriod = input.Period;
                existing.IsEnabled = input.IsEnabled;
                existing.UpdatedAt = DateTimeOffset.UtcNow;

                foreach (var oldRange in existing.Ranges.ToList())
                {
                    _automationRangeRepository.Delete(oldRange);
                }

                foreach (var range in input.Ranges)
                {
                    newRangesToInsert.Add(new PointsAutomationRange
                    {
                        AutomationSettingId = existing.Id,
                        RangeType = range.RangeType,
                        FromValue = range.FromValue,
                        ToValue = range.ToValue,
                        TaskPriority = range.TaskPriority,
                        PointsValue = range.PointsValue
                    });
                }
            }
        }

        if (newSettingsToInsert.Count > 0)
        {
            await _automationSettingRepository.AddRangeAsync(newSettingsToInsert, cancellationToken);
        }

        if (newRangesToInsert.Count > 0)
        {
            await _automationRangeRepository.AddRangeAsync(newRangesToInsert, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SaveAutomationSettingsResult(IsSuccess: true, SavedCount: normalized.Count);
    }

    private static SaveAutomationSettingsDto NormalizeRequest(SaveAutomationSettingsDto dto)
    {
        if (dto.Settings == null)
            return dto;

        var settings = dto.Settings.Select(s =>
        {
            if (s.IsEnabled == false)
                return s with { Ranges = new List<AutomationRangeDto>() };

            if (s.Ranges == null)
                return s;

            var ranges = s.Ranges.Select(r => r with
            {
                RangeType = r.RangeType?.Trim() ?? string.Empty,
                TaskPriority = string.IsNullOrWhiteSpace(r.TaskPriority) ? null : r.TaskPriority.Trim(),
                PointsValue = NormalizePointsValue(r.RangeType, r.PointsValue)
            }).ToList();

            return s with { Ranges = ranges };
        }).ToList();

        return dto with { Settings = settings };
    }

    private static int NormalizePointsValue(string? rangeType, int pointsValue)
    {
        var t = rangeType?.Trim();
        if (t != null && t.Equals("Deduction", StringComparison.OrdinalIgnoreCase))
            return -Math.Abs(pointsValue);
        if (t != null && t.Equals("Reward", StringComparison.OrdinalIgnoreCase))
            return Math.Abs(pointsValue);
        return pointsValue;
    }

    private static bool TryParseCategory(string? value, out AutomationCategory category)
    {
        category = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant() switch
        {
            "attendance" => "TimeAndAttendance",
            "timeattendance" => "TimeAndAttendance",
            "time_attendance" => "TimeAndAttendance",
            "time & attendance" => "TimeAndAttendance",
            "time-and-attendance" => "TimeAndAttendance",
            _ => value.Trim()
        };

        return Enum.TryParse(normalized, ignoreCase: true, out category)
            && Enum.IsDefined(typeof(AutomationCategory), category);
    }

    private record RangeInput(
        string RangeType,
        decimal? FromValue,
        decimal? ToValue,
        string? TaskPriority,
        int PointsValue);
}
