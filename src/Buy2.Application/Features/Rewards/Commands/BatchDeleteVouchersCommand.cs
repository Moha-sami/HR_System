using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Rewards.Commands;

public record BatchDeleteVouchersCommand(
    int Id,
    IReadOnlyCollection<int> VoucherIds
) : IRequest<Result<BatchDeleteVouchersResultDto>>;

public class BatchDeleteVouchersCommandHandler : IRequestHandler<BatchDeleteVouchersCommand, Result<BatchDeleteVouchersResultDto>>
{
    private readonly IRepository<RewardItem> _rewardItemRepository;
    private readonly IRepository<RewardVoucher> _voucherRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BatchDeleteVouchersCommandHandler(
        IRepository<RewardItem> rewardItemRepository,
        IRepository<RewardVoucher> voucherRepository,
        IUnitOfWork unitOfWork)
    {
        _rewardItemRepository = rewardItemRepository;
        _voucherRepository = voucherRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BatchDeleteVouchersResultDto>> Handle(BatchDeleteVouchersCommand command, CancellationToken cancellation)
    {
        if (command.VoucherIds is null || command.VoucherIds.Count == 0)
        {
            return Result<BatchDeleteVouchersResultDto>.ValidationFailure(
                "At least one voucher must be selected.");
        }

        var rewardItem = await _rewardItemRepository
            .Query(false)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellation);

        if (rewardItem is null)
        {
            return Result<BatchDeleteVouchersResultDto>.NotFound(
                "Reward item not found.");
        }

        var voucherIds = command.VoucherIds
            .Distinct()
            .ToList();

        var vouchers = await _voucherRepository
            .Query(false)
            .Where(v => v.RewardItemId == command.Id && voucherIds.Contains(v.Id))
            .ToListAsync(cancellation);

        var availableVouchers = vouchers
            .Where(v => v.Status == VoucherStatus.Available)
            .ToList();

        var skippedVouchers = vouchers
            .Where(v => v.Status != VoucherStatus.Available)
            .ToList();

        var notFoundCount = voucherIds.Count - vouchers.Count;
        var totalSkippedCount = skippedVouchers.Count + notFoundCount;

        if (availableVouchers.Count > 0)
        {
            foreach (var voucher in availableVouchers)
            {
                _voucherRepository.Delete(voucher);
            }

            rewardItem.AvailableStock = Math.Max(0, rewardItem.AvailableStock - availableVouchers.Count);
            await _unitOfWork.SaveChangesAsync(cancellation);
        }

        var message = totalSkippedCount > 0
            ? "Cannot delete redeemed voucher codes. Only unused available vouchers can be removed from inventory."
            : "Voucher codes deleted successfully.";

        return Result<BatchDeleteVouchersResultDto>.Success(new BatchDeleteVouchersResultDto(
            DeletedCount: availableVouchers.Count,
            SkippedCount: totalSkippedCount,
            Message: message));
    }
}