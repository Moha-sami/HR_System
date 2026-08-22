namespace Buy2.Application.Features.Employees.GetPointsSummary;

public record PointsSummaryDto(
    int CurrentBalance,
    int TotalPointsRedeemed,
    int TotalRewardsRedeemed,
    int TotalRewardsCostPoints
);
