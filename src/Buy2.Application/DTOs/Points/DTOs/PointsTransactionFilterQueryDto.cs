namespace Buy2.Application.DTOs.Points.DTOs;

public record PointsTransactionFilterQueryDto(
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    string? Period,
    string? SearchTerm,
    string? TriggeredBy,
    string? TransactionType,
    string? SortBy,
    string? SortDir,
    int PageNumber = 1,
    int PageSize = 10,
    int? Month = null,
    int? Year = null
);