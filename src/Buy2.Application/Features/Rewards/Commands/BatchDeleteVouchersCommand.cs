using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Buy2.Application.Features.Rewards.Commands;

public record BatchDeleteVouchersCommand(
    int Id,
    IReadOnlyCollection<int> VoucherIds
) : IRequest<BatchDeleteVouchersResultDto>;

public class BatchDeleteVouchersCommandHandler : IRequestHandler<BatchDeleteVouchersCommand, BatchDeleteVouchersResultDto>
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

    public async Task<BatchDeleteVouchersResultDto> Handle(BatchDeleteVouchersCommand command, CancellationToken cancellation)
    {
        var rewardItem = await _rewardItemRepository
                   .Query(false)
                   .FirstOrDefaultAsync(
                       r => r.Id == command.Id,
                       cancellation);

        if (rewardItem is null)
        {
            throw new ValidationException(
                "Reward item not found.");
        }

        if (command.VoucherIds is null ||
            command.VoucherIds.Count == 0)
        {
            throw new ValidationException(
                "At least one voucher must be selected.");
        }

        var voucherIds = command.VoucherIds
            .Distinct()
            .ToList();

        var vouchers = await _voucherRepository
            .Query(false)
            .Where(v =>
                v.RewardItemId == command.Id &&
                voucherIds.Contains(v.Id))
            .ToListAsync(cancellation);

        // 4. Partition vouchers
        var availableVouchers = vouchers
            .Where(v => v.Status == VoucherStatus.Available)
            .ToList();

        var skippedVouchers = vouchers
            .Where(v => v.Status != VoucherStatus.Available)
            .ToList();

        // 5. Delete available vouchers only
        if (availableVouchers.Count > 0)
        {
            // 5. Delete available vouchers
            foreach (var voucher in availableVouchers)
            {
                _voucherRepository.Delete(voucher);
            }
        }

        var allVouchers = await _voucherRepository
            .Query(false)
            .Where(v => v.RewardItemId == command.Id)
            .ToListAsync(cancellation);

        var deletedVoucherIds = availableVouchers
            .Select(v => v.Id)
            .ToHashSet();

        rewardItem.AvailableStock = allVouchers
            .Count(v =>
                v.Status == VoucherStatus.Available &&
                !deletedVoucherIds.Contains(v.Id));

        await _unitOfWork.SaveChangesAsync(cancellation);

        var message = skippedVouchers.Count > 0
            ? "Cannot delete redeemed voucher codes. Only unused available vouchers can be removed from inventory."
            : "Voucher codes deleted successfully.";

        return new BatchDeleteVouchersResultDto(
            DeletedCount: availableVouchers.Count,
            SkippedCount: skippedVouchers.Count,
            Message: message);
    }
}