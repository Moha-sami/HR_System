using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Buy2.Application.Features.Rewards.Commands;

public record DeleteRewardCommand(int Id) : IRequest;
public class DeleteRewardCommandHandler : IRequestHandler<DeleteRewardCommand>
{
    private readonly IRepository<RewardItem> _rewardItemRepository;
    private readonly IRepository<RewardVoucher> _voucherRepository;
    private readonly IRepository<RewardRedemption> _redempationRepository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteRewardCommandHandler(IRepository<RewardItem> rewardItem, IRepository<RewardVoucher> voucher, IRepository<RewardRedemption> redempation, IUnitOfWork unit)
    {
        _rewardItemRepository = rewardItem;
        _voucherRepository = voucher;
        _redempationRepository = redempation;
        _unitOfWork = unit;
    }
    public async Task Handle(DeleteRewardCommand command, CancellationToken cancellation)
    {
        var rewardItem = await _rewardItemRepository
        .Query()
        .FirstOrDefaultAsync(r => r.Id == command.Id, cancellation);

        if (rewardItem is null)
        {
            throw new ValidationException("Reward Item not found.");
        }

        var availableVouchers = await _voucherRepository
            .Query(false)
            .CountAsync(v => v.RewardItemId == command.Id &&
                             v.Status == VoucherStatus.Available, cancellation);


        if (availableVouchers > 0 || rewardItem.AvailableStock > 0)
        {
            throw new ValidationException($"Cannot delete reward because it currently contains active voucher inventory ({availableVouchers} available codes). Please delete or clear remaining available vouchers first from the inventory management drawer, or set the reward to Inactive.");
        }


        var redempationCount = await _redempationRepository
            .Query(false)
            .CountAsync(r => r.RewardItemId == command.Id, cancellation);

        if (redempationCount > 0)
        {
            rewardItem.IsActive = false;
        }
        else
        {
            _rewardItemRepository.Delete(rewardItem);
        }
        await _unitOfWork.SaveChangesAsync(cancellation);
    }
}