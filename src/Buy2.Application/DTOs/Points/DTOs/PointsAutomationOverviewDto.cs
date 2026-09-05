namespace Buy2.Application.DTOs.Points.DTOs;

public record PointsAutomationOverviewDto(
    string AutomationPeriod,
    AutomationCategoryDto Performance,
    TaskCategoryDto Tasks,
    AttendanceCategoryDto TimeAndAttendance
);

public record AutomationCategoryDto(
    string Category,
    string AutomationPeriod,
    List<AutomationSettingCategoryDto> Rules
);

public record TaskCategoryDto(
    string Category,
    string AutomationPeriod,
    List<AutomationSettingCategoryDto> CompletionRules,
    List<AutomationSettingCategoryDto> DeadlineRules
);

public record AttendanceCategoryDto(
    string Category,
    string AutomationPeriod,
    List<AutomationSettingCategoryDto> AttendanceRateRules,
    List<AutomationSettingCategoryDto> LatenessRules
);
