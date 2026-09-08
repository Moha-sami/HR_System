namespace Buy2.Application.DTOs.Rewards.DTOs;

// Reward Profile List
public record RewardProfileListDto(
    int Id,
    string Name,
    string? Description,
    string? ImageUrl,
    string Category,
    int Points,
    decimal MonetaryValue,
    string HowToRedeem,
    string TermsOfUse,
    bool IsActive
);

// Reward KPI Statistics Card:
public record RewardKpiStatistics(
    int RedemptionCount,
    int AvailableStock,
    decimal TotalCost,
    int TopRedeemed,
    int PointsValue
);
// Reward Analytics Chart: 
public record RewardRedemptionAnalyticsChartDto(
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int TotalRedemptions
);
// Reward Transaction History Log:
public record RewardTransactionHistoryLogDto(
    int TransactionId,
    string EmployeeName,
    DateTimeOffset Date,
    DateTime Time,
    string Code
);