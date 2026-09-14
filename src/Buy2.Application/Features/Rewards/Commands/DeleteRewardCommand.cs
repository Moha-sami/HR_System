using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Rewards.Commands;

public record DeleteRewardCommand(int Id) : IRequest<Result>;

public class DeleteRewardCommandHandler : IRequestHandler<DeleteRewardCommand, Result>
{
    private readonly IRepository<RewardItem> _rewardItemRepository;
    private readonly IRepository<RewardVoucher> _voucherRepository;
    private readonly IRepository<RewardRedemption> _redemptionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRewardCommandHandler(
        IRepository<RewardItem> rewardItemRepository,
        IRepository<RewardVoucher> voucherRepository,
        IRepository<RewardRedemption> redemptionRepository,
        IUnitOfWork unitOfWork)
    {
        _rewardItemRepository = rewardItemRepository;
        _voucherRepository = voucherRepository;
        _redemptionRepository = redemptionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteRewardCommand command, CancellationToken cancellation)
    {
        var rewardItem = await _rewardItemRepository
            .Query(false)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellation);

        if (rewardItem is null)
        {
            return Result.NotFound("Reward item not found.");
        }

        var availableVouchers = await _voucherRepository
            .Query(false)
            .CountAsync(v => v.RewardItemId == command.Id &&
                             v.Status == VoucherStatus.Available, cancellation);

        if (availableVouchers > 0 || rewardItem.AvailableStock > 0)
        {
            return Result.Conflict($"Cannot delete reward because it currently contains active voucher inventory ({availableVouchers} available codes). Please delete or clear remaining available vouchers first from the inventory management drawer, or set the reward to Inactive.");
        }

        var redemptionCount = await _redemptionRepository
            .Query(false)
            .CountAsync(r => r.RewardItemId == command.Id, cancellation);

        if (redemptionCount > 0)
        {
            rewardItem.IsActive = false;
            _rewardItemRepository.Update(rewardItem);
        }
        else
        {
            _rewardItemRepository.Delete(rewardItem);
        }

        await _unitOfWork.SaveChangesAsync(cancellation);
        return Result.Success();
    }
}