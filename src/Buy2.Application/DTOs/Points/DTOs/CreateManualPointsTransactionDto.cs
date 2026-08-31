namespace Buy2.Application.DTOs.Points.DTOs;

public record CreateManualPointsTransactionDto(
    int EmployeeId,
    string TransactionType,
    decimal PointsValue,
    string Comments
);