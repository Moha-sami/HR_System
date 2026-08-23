namespace Buy2.Application.Features.Employees.GetViolations;

public record ViolationDto(
    int Id,
    int EmployeeId,
    string Type,
    string Severity,
    string Description,
    string Status,
    string ReportedByName,
    DateTimeOffset CreatedAt,
    string? ActionType,
    DateTime? ActionDate,
    string? DocumentUrl
);
