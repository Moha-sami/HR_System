namespace Buy2.Application.Features.Employees.ResolveViolation;

public record ResolveViolationDto(
    string ActionType,
    string ActionDescription,
    DateTime? ActionDate = null,
    int? ActionTakenById = null
);
