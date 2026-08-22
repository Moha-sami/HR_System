namespace Buy2.Application.Features.Employees.GetViolationDetail;

public record ViolationActionDetailDto(
    string? ActionType,
    DateTime? ActionDate,
    string? ActionTakenByName,
    string? ActionDescription
);

public record ViolationDetailDto(
    int Id,
    int EmployeeId,
    string ViolationType,
    string Severity,
    string Description,
    string Status,
    string ReportedByName,
    List<string> Witnesses,
    string? DocumentUrl,
    DateTimeOffset CreatedAt,
    ViolationActionDetailDto? ActionDetail
);
