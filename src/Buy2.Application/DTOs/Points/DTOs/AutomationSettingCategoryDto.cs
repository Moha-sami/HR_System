namespace Buy2.Application.DTOs.Points.DTOs;

public record AutomationSettingCategoryDto(
    int Id,
    string Category,
    string SubCategory,
    bool IsEnabled,
    List<AutomationRangeDto> Ranges
);