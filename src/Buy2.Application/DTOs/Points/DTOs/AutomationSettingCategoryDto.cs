namespace Buy2.Application.DTOs.Points.DTOs;

public record AutomationSettingCategoryDto(
    int Id,
    string Category,
    string SubCategory,
    string AutomationPeriod,
    bool? IsEnabled,
    List<AutomationRangeDto> Ranges
);
