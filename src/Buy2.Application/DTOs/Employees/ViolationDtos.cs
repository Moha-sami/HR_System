using Buy2.Domain.Enums;

namespace Buy2.Application.DTOs.Employees;

// Original log violation request DTO
public record LogDisciplinaryViolationDto(
    int EmployeeId,
    string Severity,
    string Description
);

// 1. Violation List Item DTO for Table Row
public record ViolationListItemDto(
    int Id,
    DateTime Date,
    ViolationType ViolationType,
    string Description,
    string ReportedBy,
    string SeverityLevel,
    ViolationStatus Status
);

// 2. Action Taken Section DTO (All fields except Status nullable for pending state)
public record ViolationActionTakenDto(
    ViolationStatus Status,
    DateTime? ActionDate = null,
    string? ActionType = null,
    string? TakenBy = null,
    string? Description = null
);

// 3. Full Violation Detail DTO
public record ViolationDetailDto(
    int Id,
    ViolationType ViolationType,
    DateTime Date,
    string ReportedBy,
    string SeverityLevel,
    List<string> Witnesses,
    string? DocumentUrl,
    ViolationActionTakenDto ActionTaken
);

// 4. Resolve Violation Input DTO (Request Body for PATCH endpoint)
public record ResolveViolationRequestDto(
    string ActionType,
    DateTime ActionDate,
    int ActionTakenById,
    string ActionDescription
);