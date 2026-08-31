namespace Buy2.Application.DTOs.Points.DTOs;

public record UpdateAutomationCategoryDto(
    string Category,
    string SubCategory,
    bool IsEnabled,
    List<AutomationRangeDto> Ranges
);