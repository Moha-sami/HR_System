namespace Buy2.Application.DTOs.Rewards.DTOs;

// Reward List
public record RewardListDto(
    int Id,
    string Name,
    string Category,
    int Points,
    decimal MonetaryValue,
    string StockRatio,
    int RedemptionCount,
    bool IsActive
);

// Reward Filter
public record RewardFilterDto(
    string? Search,
    string? Status,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    int Page = 1,
    int PageSize = 10
);

// Reward Create
public record RewardCreateDto(
    string Name,
    string? Description,
    int CategoryId,
    string? BannerImageUrl,
    int Points,
    decimal MonetaryValue,
    string HowToRedeem,
    string TermsOfUse
);

// Reward Update
public record RewardUpdateDto(
    string Name, 
    string? Description,
    int CategoryId,
    string? BannerImageUrl,
    int Points,
    decimal MonetaryValue,
    string HowToRedeem,
    string TermsOfUse
);

// Page Result
public record PageResultDto<T>(
    ICollection<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);