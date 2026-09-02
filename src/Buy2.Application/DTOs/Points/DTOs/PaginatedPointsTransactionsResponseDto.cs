namespace Buy2.Application.DTOs.Points.DTOs;

public record PaginatedPointsTransactionsResponseDto(
    List<PointsTransactionListItemDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);