namespace Buy2.Application.DTOs.Points.DTOs;

public record ExecuteAutomationJobDto(
    string Category,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    List<int>? TargetEmployeeIds
);