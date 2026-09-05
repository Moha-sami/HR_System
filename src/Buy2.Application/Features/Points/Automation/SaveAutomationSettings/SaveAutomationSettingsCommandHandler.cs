using Buy2.Application.Common.Interfaces;
using Buy2.Application.Validators.Points;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        var dto = request.Request;

        var validation = await new SaveAutomationSettingsDtoValidator().ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return new SaveAutomationSettingsResult(
                IsSuccess: false,
                ErrorMessage: string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var normalized = new List<(AutomationCategory Category, string SubCategory, AutomationPeriod Period, bool IsEnabled, List<RangeInput> Ranges)>();
        foreach (var setting in dto.Settings!)
        {
            if (!TryParseCategory(setting.Category, out var category))
            {
                return new SaveAutomationSettingsResult(
                    IsSuccess: false,
                    ErrorMessage: $"Unknown category '{setting.Category}'. Must be one of: Performance, Tasks, TimeAndAttendance.");
            }

            if (!Enum.TryParse<AutomationPeriod>(setting.AutomationPeriod?.Trim(), ignoreCase: true, out var period))
            {
                return new SaveAutomationSettingsResult(
                    IsSuccess: false,
                    ErrorMessage: $"AutomationPeriod must be one of: Daily, Weekly, BiWeekly, Monthly (category '{setting.Category}').");
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

            normalized.Add((category, subCategory, period, setting.IsEnabled.Value,
                setting.Ranges!.Select(r => new RangeInput(
                    r.RangeType.Trim(),
                    r.FromValue,
                    r.ToValue,
                    string.IsNullOrWhiteSpace(r.TaskPriority) ? null : r.TaskPriority.Trim(),
                    r.PointsValue)).ToList()));
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
            .Where(s => s.Category == AutomationCategory.Tasks
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

        var existingSettings = await _automationSettingRepository.Query(asNoTracking: false)
            .Include(s => s.Ranges)
            .Where(s => normalized.Select(n => n.Category).Contains(s.Category)
                && normalized.Select(n => n.SubCategory.ToUpper()).Contains(s.SubCategory.ToUpper()))
            .ToListAsync(cancellationToken);

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

                await _automationSettingRepository.AddAsync(created);
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
                    await _automationRangeRepository.AddAsync(new PointsAutomationRange
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SaveAutomationSettingsResult(IsSuccess: true, SavedCount: normalized.Count);
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
