namespace Buy2.Application.DTOs.Points.DTOs;

public record SaveAutomationSettingsDto(
    string AutomationPeriod,
    List<AutomationSettingCategoryDto> Settings
);