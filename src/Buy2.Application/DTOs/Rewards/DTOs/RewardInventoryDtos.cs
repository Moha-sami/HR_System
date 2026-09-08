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