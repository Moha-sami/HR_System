namespace Buy2.Application.DTOs.Points.DTOs;

public record AutomationRangeDto(
    int? Id,
    string RangeType,
    decimal? FromValue,
    decimal? ToValue,
    string? TaskPriority,
    int PointsValue
);