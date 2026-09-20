using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Buy2.Application.Features.Rewards.Commands;

public record UploadRewardVouchersCommand(
    int Id, 
    UploadVouchersRequestDto dto
) : IRequest<UploadVouchersResponseDto>;
public class UploadRewardVouchersCommandHandler : IRequestHandler<UploadRewardVouchersCommand, UploadVouchersResponseDto>
{
    private readonly IRepository<RewardItem> _rewardItemRepository;
    private readonly IRepository<RewardVoucher> _voucherRepository;
    private readonly IExcelVoucherParserService _voucherParserService;
    private readonly IUnitOfWork _unitOfWork;
    public UploadRewardVouchersCommandHandler(IRepository<RewardItem> item, IRepository<RewardVoucher> voucher, IExcelVoucherParserService voucherParserService, IUnitOfWork unit)
    {
        _rewardItemRepository   = item;
        _voucherRepository      = voucher;
        _voucherParserService   = voucherParserService;
        _unitOfWork             = unit;
    }
    public async Task<UploadVouchersResponseDto> Handle(UploadRewardVouchersCommand command, CancellationToken cancellation)
    {
        var rewardItem = await _rewardItemRepository
                  .Query(false)
                  .FirstOrDefaultAsync(
                      r => r.Id == command.Id,
                      cancellation);

        if (rewardItem is null)
        {
            throw new ValidationException("Reward item not found.");
        }

        var file = command.dto.File;

        if (file is null || file.Length == 0)
        {
            throw new ValidationException("File not found.");
        }

        var allowedExtensions = new[]
        {
            ".xlsx",
            ".xls",
            ".csv"
        };

        var extension = Path
            .GetExtension(file.FileName)
            .ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            throw new ValidationException(
                "Invalid file format. Only Excel (.xlsx, .xls) and CSV (.csv) are supported for voucher inventory upload.");
        }
        var batchId = string.IsNullOrWhiteSpace(command.dto.BatchId)
            ? Random.Shared.Next(100000, 999999)
            : int.Parse(command.dto.BatchId);

        var codes = await _voucherParserService
            .UploadExcelFileAsync(file, cancellation);

        if (codes.Count == 0)
        {
            throw new ValidationException("No voucher codes were found in the uploaded file.");
        }

        var normalizedCodes = codes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .ToList();

        var totalFound = normalizedCodes.Count;

        var uniqueCodes = normalizedCodes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var duplicateInFileCount =
            totalFound - uniqueCodes.Count;

        var existingCodes = await _voucherRepository
            .Query(true)
            .Where(v =>
                v.RewardItemId == command.Id &&
                uniqueCodes.Contains(v.Code))
            .Select(v => v.Code)
            .ToListAsync(cancellation);

        var existingCodeSet = existingCodes
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var duplicateInDbCount = existingCodeSet.Count;

        var validCodes = uniqueCodes
            .Where(code => !existingCodeSet.Contains(code))
            .ToList();


        if (!command.dto.Confirm)
        {
            var previewItems = uniqueCodes
                .Select(code =>
                {
                    var existsInDb = existingCodeSet.Contains(code);

                    return new VoucherUploadPreviewItemDto(
                        code,
                        !existsInDb,
                        existsInDb
                            ? "Voucher already exists."
                            : null);
                })
                .ToList();

            var preview = new VoucherUploadPreviewDto(
                BatchId: batchId,
                TotalFound: totalFound,
                ValidCount: validCodes.Count,
                DuplicateInFileCount: duplicateInFileCount,
                DuplicateInDbCount: duplicateInDbCount,
                Preview: previewItems);

            return new UploadVouchersResponseDto(
                Preview: preview,
                Result: null);
        }

        if (validCodes.Count == 0)
        {
            throw new ValidationException(
                "No valid vouchers are available for upload.");
        }

        
        var uploadedByUserId = GetCurrentUserId();

        var vouchers = validCodes
            .Select(code => new RewardVoucher
            {
                RewardItemId = command.Id,
                Code = code,
                BatchId = batchId,
                Status = VoucherStatus.Available,
            })
            .ToList();

        await _voucherRepository.AddRangeAsync(
            vouchers,
            cancellation);

        
        rewardItem.AvailableStock += vouchers.Count;

        
        await _unitOfWork.SaveChangesAsync(cancellation);

        var result = new VoucherUploadResultDto(
            BatchId: batchId,
            TotalUploaded: vouchers.Count,
            AvailableStock: rewardItem.AvailableStock,
            ExpiryDate: command.dto.ExpiryDate);

        return new UploadVouchersResponseDto(
            Preview: null,
            Result: result);
    }
    private int GetCurrentUserId()
    {
        throw new NotImplementedException();
    }
}