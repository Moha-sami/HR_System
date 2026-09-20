using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Rewards.Commands;

public record UploadRewardVouchersCommand(
    int Id,
    UploadVouchersRequestDto Dto
) : IRequest<Result<UploadVouchersResponseDto>>;

public class UploadRewardVouchersCommandHandler : IRequestHandler<UploadRewardVouchersCommand, Result<UploadVouchersResponseDto>>
{
    private readonly IRepository<RewardItem> _rewardItemRepository;
    private readonly IRepository<RewardVoucher> _voucherRepository;
    private readonly IExcelVoucherParserService _voucherParserService;
    private readonly IUnitOfWork _unitOfWork;

    public UploadRewardVouchersCommandHandler(
        IRepository<RewardItem> item,
        IRepository<RewardVoucher> voucher,
        IExcelVoucherParserService voucherParserService,
        IUnitOfWork unit)
    {
        _rewardItemRepository = item;
        _voucherRepository = voucher;
        _voucherParserService = voucherParserService;
        _unitOfWork = unit;
    }

    public async Task<Result<UploadVouchersResponseDto>> Handle(UploadRewardVouchersCommand command, CancellationToken cancellation)
    {
        if (command.Dto is null)
        {
            return Result<UploadVouchersResponseDto>.ValidationFailure("Request data cannot be empty.");
        }

        var isCommit = command.Dto.Confirm;

        var rewardItem = await _rewardItemRepository
            .Query(!isCommit)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellation);

        if (rewardItem is null)
        {
            return Result<UploadVouchersResponseDto>.NotFound("Reward item not found.");
        }

        var file = command.Dto.File;
        if (file is null || file.Length == 0)
        {
            return Result<UploadVouchersResponseDto>.ValidationFailure("File not found or empty.");
        }

        var allowedExtensions = new[] { ".xlsx", ".xls", ".csv" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return Result<UploadVouchersResponseDto>.ValidationFailure(
                "Invalid file format. Only Excel (.xlsx, .xls) and CSV (.csv) are supported for voucher inventory upload.");
        }

        int batchId;
        if (string.IsNullOrWhiteSpace(command.Dto.BatchId) || !int.TryParse(command.Dto.BatchId, out batchId))
        {
            batchId = Random.Shared.Next(100000, 999999);
        }

        List<string> codes;
        try
        {
            codes = await _voucherParserService.UploadExcelFileAsync(file, cancellation);
        }
        catch (Exception ex)
        {
            return Result<UploadVouchersResponseDto>.ValidationFailure($"Failed to parse voucher file: {ex.Message}");
        }

        if (codes.Count == 0)
        {
            return Result<UploadVouchersResponseDto>.ValidationFailure("No voucher codes were found in the uploaded file.");
        }

        var normalizedCodes = codes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .ToList();

        var totalFound = normalizedCodes.Count;

        var uniqueCodes = normalizedCodes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var duplicateInFileCount = totalFound - uniqueCodes.Count;

        var existingCodes = await _voucherRepository
            .Query(true)
            .Where(v => v.RewardItemId == command.Id && uniqueCodes.Contains(v.Code))
            .Select(v => v.Code)
            .ToListAsync(cancellation);

        var existingCodeSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var duplicateInDbCount = existingCodeSet.Count;

        var validCodes = uniqueCodes
            .Where(code => !existingCodeSet.Contains(code))
            .ToList();

        if (!command.Dto.Confirm)
        {
            var previewItems = uniqueCodes
                .Select(code =>
                {
                    var existsInDb = existingCodeSet.Contains(code);
                    return new VoucherUploadPreviewItemDto(
                        code,
                        !existsInDb,
                        existsInDb ? "Voucher already exists." : null);
                })
                .ToList();

            var preview = new VoucherUploadPreviewDto(
                BatchId: batchId,
                TotalFound: totalFound,
                ValidCount: validCodes.Count,
                DuplicateInFileCount: duplicateInFileCount,
                DuplicateInDbCount: duplicateInDbCount,
                Preview: previewItems);

            return Result<UploadVouchersResponseDto>.Success(new UploadVouchersResponseDto(
                Preview: preview,
                Result: null));
        }

        if (validCodes.Count == 0)
        {
            return Result<UploadVouchersResponseDto>.ValidationFailure("No valid vouchers are available for upload.");
        }

        var vouchers = validCodes
            .Select(code => new RewardVoucher
            {
                RewardItemId = command.Id,
                Code = code,
                BatchId = batchId,
                Status = VoucherStatus.Available,
            })
            .ToList();

        await _voucherRepository.AddRangeAsync(vouchers, cancellation);

        rewardItem.AvailableStock += vouchers.Count;

        await _unitOfWork.SaveChangesAsync(cancellation);

        var result = new VoucherUploadResultDto(
            BatchId: batchId,
            TotalUploaded: vouchers.Count,
            AvailableStock: rewardItem.AvailableStock,
            ExpiryDate: command.Dto.ExpiryDate);

        return Result<UploadVouchersResponseDto>.Success(new UploadVouchersResponseDto(
            Preview: null,
            Result: result));
    }
}