using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Points.GetAutomationRules;

public record GetPointsAutomationRulesQuery : IRequest<GetPointsAutomationRulesResult>;

public record GetPointsAutomationRulesResult(
    bool IsSuccess,
    PointsAutomationOverviewDto? Data = null,
    string? ErrorMessage = null,
    bool IsNotFound = false);

public class GetPointsAutomationRulesQueryHandler : IRequestHandler<GetPointsAutomationRulesQuery, GetPointsAutomationRulesResult>
{
    private readonly IRepository<PointsAutomationSetting> _automationSettingRepository;

    public GetPointsAutomationRulesQueryHandler(IRepository<PointsAutomationSetting> automationSettingRepository)
    {
        _automationSettingRepository = automationSettingRepository;
    }

    public async Task<GetPointsAutomationRulesResult> Handle(GetPointsAutomationRulesQuery request, CancellationToken cancellationToken)
    {
        var settings = await _automationSettingRepository.Query()
            .AsNoTracking()
            .Include(s => s.Ranges)
            .Where(s => s.IsEnabled)
            .ToListAsync(cancellationToken);

        if (!settings.Any())
        {
            return new GetPointsAutomationRulesResult(
                IsSuccess: false,
                ErrorMessage: "No enabled automation settings found.",
                IsNotFound: true);
        }

        var automationPeriod = settings.First().AutomationPeriod.ToString();

        var performance = MapPerformance(settings);
        var tasks = MapTasks(settings);
        var timeAndAttendance = MapTimeAndAttendance(settings);

        var data = new PointsAutomationOverviewDto(
            AutomationPeriod: automationPeriod,
            Performance: performance,
            Tasks: tasks,
            TimeAndAttendance: timeAndAttendance
        );

        return new GetPointsAutomationRulesResult(
            IsSuccess: true,
            Data: data);
    }

    private static AutomationCategoryDto MapPerformance(List<PointsAutomationSetting> settings)
    {
        var performanceSettings = settings.Where(s => s.Category == AutomationCategory.Performance).ToList();
        var rules = performanceSettings.Select(MapSettingToDto).ToList();

        return new AutomationCategoryDto(
            Category: "Performance",
            Rules: rules
        );
    }

    private static TaskCategoryDto MapTasks(List<PointsAutomationSetting> settings)
    {
        var taskSettings = settings.Where(s => s.Category == AutomationCategory.Tasks).ToList();

        var completionRules = taskSettings
            .Where(s => s.SubCategory.Equals("Completion", StringComparison.OrdinalIgnoreCase))
            .Select(MapSettingToDto)
            .ToList();

        var deadlineRules = taskSettings
            .Where(s => s.SubCategory.Equals("Deadline", StringComparison.OrdinalIgnoreCase))
            .Select(MapSettingToDto)
            .ToList();

        return new TaskCategoryDto(
            Category: "Tasks",
            CompletionRules: completionRules,
            DeadlineRules: deadlineRules
        );
    }

    private static AttendanceCategoryDto MapTimeAndAttendance(List<PointsAutomationSetting> settings)
    {
        var attendanceSettings = settings.Where(s => s.Category == AutomationCategory.TimeAndAttendance).ToList();

        var attendanceRateRules = attendanceSettings
            .Where(s => s.SubCategory.Equals("AttendanceRate", StringComparison.OrdinalIgnoreCase))
            .Select(MapSettingToDto)
            .ToList();

        var latenessRules = attendanceSettings
            .Where(s => s.SubCategory.Equals("Lateness", StringComparison.OrdinalIgnoreCase))
            .Select(MapSettingToDto)
            .ToList();

        return new AttendanceCategoryDto(
            Category: "Time & Attendance",
            AttendanceRateRules: attendanceRateRules,
            LatenessRules: latenessRules
        );
    }

    private static AutomationSettingCategoryDto MapSettingToDto(PointsAutomationSetting setting)
    {
        var ranges = setting.Ranges
            .Select(r => new AutomationRangeDto(
                Id: r.Id,
                RangeType: r.RangeType,
                FromValue: r.FromValue,
                ToValue: r.ToValue,
                TaskPriority: r.TaskPriority,
                PointsValue: r.PointsValue
            ))
            .ToList();

        return new AutomationSettingCategoryDto(
            Id: setting.Id,
            Category: setting.Category.ToString(),
            SubCategory: setting.SubCategory,
            IsEnabled: setting.IsEnabled,
            Ranges: ranges
        );
    }
}