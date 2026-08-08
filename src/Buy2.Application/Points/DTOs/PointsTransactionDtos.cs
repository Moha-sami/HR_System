namespace Buy2.Application.Points.DTOs;

public record PointsTransactionDto(int EmployeeId, int? PointsRuleId, decimal Amount, string TransactionType);