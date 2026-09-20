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


// Upload Excel File
public record UploadVouchersRequestDto(
    IFormFile File,
    string? BatchId,
    DateTimeOffset? ExpiryDate,
    bool Confirm
);
public record VoucherUploadPreviewItemDto(
    string VoucherCode,
    bool IsValid,
    string? Error
);
public record VoucherUploadPreviewDto(
    int BatchId,
    int TotalFound,
    int ValidCount,
    int DuplicateInFileCount,
    int DuplicateInDbCount,
    IReadOnlyCollection<VoucherUploadPreviewItemDto> Preview
);

public record VoucherUploadResultDto(
    int BatchId,
    int TotalUploaded,
    int AvailableStock,
    DateTimeOffset? ExpiryDate
);

public record UploadVouchersResponseDto(
    VoucherUploadPreviewDto? Preview,
    VoucherUploadResultDto? Result
);