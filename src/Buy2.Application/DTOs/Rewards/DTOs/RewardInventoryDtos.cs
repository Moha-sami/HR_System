using Buy2.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Buy2.Application.DTOs.Rewards.DTOs;

// Reward Inventory List
public record RewardInventoryListDto(
    int Id,
    int BatchId,
    DateTimeOffset Date,
    string VoucherCode,
    VoucherStatus Status
);

// Reward Bulk Excel Upload
public record RewardBulkExcelUpload(
    int RewardItemId,
    IFormFile File
);

// Batch Code Deletion
public record BatchCodeDeletion(
    int RewardItemId,
    List<string> VoucherCodes
);

public record PaginatedVouchersResponseDto(
    PageResultDto<RewardInventoryListDto> Vouchers,
    int AvailableCount,
    int RedeemedCount,
    int ExpiredCount
);

public record VoucherInventoryFilterQueryDto(
    int? BatchId = null,
    string? VoucherCode = null,
    string? Status = null,
    string? SortBy = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    int Page = 1,
    int PageSize = 10
);