namespace Buy2.Application.DTOs.Employees;

public record DocumentDtos(int EmployeeId, string Category, string StorageUrl);

public record EmployeeDocumentDto(
    int Id,
    int EmployeeId,
    string Category,
    string StorageUrl,
    DateTime CreatedAt);
