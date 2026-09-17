namespace Buy2.Application.DTOs.Rewards.DTOs;

public record RewardTransactionFilterQueryDto(
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    string? TimelinePeriod = "Monthly",
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10
);

public record RedemptionTimelinePointDto(
    string PeriodLabel,
    DateTimeOffset DateFrom,
    DateTimeOffset DateTo,
    int RedemptionCount,
    int TotalPointsSpent
);

public record RewardTransactionItemDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    string EmployeeCode,
    string DepartmentName,
    string VoucherCode,
    DateTimeOffset RedeemedAt,
    TimeSpan Time,
    int PointsDeducted
);

public record RewardAnalyticsDto(
    IReadOnlyCollection<RedemptionTimelinePointDto> Timeline,
    IReadOnlyCollection<RewardTransactionItemDto> Transactions,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);