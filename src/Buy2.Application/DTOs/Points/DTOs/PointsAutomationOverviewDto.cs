namespace Buy2.Application.DTOs.Points.DTOs;

public record PointsAutomationOverviewDto(
    string AutomationPeriod,
    AutomationSettingCategoryDto PerformanceSettings,
    AutomationSettingCategoryDto TaskSettings,
    AutomationSettingCategoryDto AttendanceSettings
);