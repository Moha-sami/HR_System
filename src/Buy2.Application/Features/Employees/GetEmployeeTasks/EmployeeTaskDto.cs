namespace Buy2.Application.Features.Employees.GetEmployeeTasks;

public record EmployeeTaskDto(
    int Id,
    int EmployeeId,
    string Title,
    string? Description,
    string Status,
    string? Priority,
    DateTimeOffset? DueDate,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt
);
