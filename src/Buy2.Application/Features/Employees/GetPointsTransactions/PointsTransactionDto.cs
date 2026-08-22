namespace Buy2.Application.Features.Employees.GetPointsTransactions;

public record PointsTransactionDto(
    int Id,
    DateTimeOffset Date,
    int Amount,
    string Type,
    string? TriggeredBy,
    string? Comments
);

public record PaginatedPointsTransactionsDto(
    List<PointsTransactionDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
