namespace Buy2.Application.DTOs.Points.DTOs;

public record PointsTransactionListItemDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    string EmployeeCode,
    string DepartmentName,
    string SiteName,
    string? AvatarUrl,
    DateTimeOffset Date,
    TimeSpan Time,
    string TransactionType,
    int Points,
    string TriggeredBy,
    string? Comments,
    DateTimeOffset CreatedAt
);