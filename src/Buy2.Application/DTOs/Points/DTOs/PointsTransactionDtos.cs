namespace Buy2.Application.DTOs.Points.DTOs;

public record PointsTransactionDto(int EmployeeId, int? PointsRuleId, decimal Amount, string TransactionType);