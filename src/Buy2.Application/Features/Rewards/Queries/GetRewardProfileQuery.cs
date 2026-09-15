using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Rewards.Queries;

public record GetRewardProfileQuery(int Id) : IRequest<Result<RewardProfileResponseDto>>;

public class GetRewardProfileQueryHandler : IRequestHandler<GetRewardProfileQuery, Result<RewardProfileResponseDto>>
{
    private readonly IRepository<RewardItem> _rewardItemRepository;
    private readonly IRepository<RewardRedemption> _redemptionRepository;
    private readonly IRepository<RewardVoucher> _voucherRepository;

    public GetRewardProfileQueryHandler(
        IRepository<RewardItem> item,
        IRepository<RewardRedemption> redemption,
        IRepository<RewardVoucher> voucher)
    {
        _rewardItemRepository = item;
        _redemptionRepository = redemption;
        _voucherRepository = voucher;
    }

    public async Task<Result<RewardProfileResponseDto>> Handle(GetRewardProfileQuery query, CancellationToken cancellation)
    {
        var rewardItem = await _rewardItemRepository
            .Query(true)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellation);

        if (rewardItem is null)
        {
            return Result<RewardProfileResponseDto>.NotFound("Reward item not found.");
        }

        var redemptionCount = await _redemptionRepository
            .Query(true)
            .CountAsync(r => r.RewardItemId == query.Id, cancellation);

        var totalVouchers = await _voucherRepository
            .Query(true)
            .CountAsync(v => v.RewardItemId == query.Id, cancellation);

        var availableVouchers = await _voucherRepository
            .Query(true)
            .CountAsync(v => v.RewardItemId == query.Id &&
                             v.Status == VoucherStatus.Available,
                             cancellation);

        var availabilityStock = $"{availableVouchers}/{totalVouchers}";

        var totalCost = await _redemptionRepository
            .Query(true)
            .Where(r => r.RewardItemId == query.Id && r.PointsTransaction != null)
            .Select(r => (decimal)Math.Abs(r.PointsTransaction.Amount))
            .SumAsync(cancellation);

        var topRedeem = await _redemptionRepository
            .Query(true)
            .Where(r =>
                r.RewardItemId == query.Id &&
                r.Employee != null &&
                r.Employee.JobRole != null &&
                r.Employee.JobRole.Department != null)
            .GroupBy(r => r.Employee.JobRole!.Department!.Name)
            .Select(d => new
            {
                Department = d.Key,
                RedemptionCount = d.Count()
            })
            .OrderByDescending(d => d.RedemptionCount)
            .FirstOrDefaultAsync(cancellation);

        var topRedeemedValue = topRedeem?.RedemptionCount ?? 0;

        var kpiStats = new RewardKpiStatistics(
            RedemptionCount: redemptionCount,
            AvailableStock: availabilityStock,
            TotalCost: totalCost,
            TopRedeemed: topRedeemedValue,
            PointsValue: rewardItem.CostInPoints
        );

        var rewardProfile = new RewardProfileListDto(
            rewardItem.Id,
            rewardItem.RewardName,
            rewardItem.Description,
            rewardItem.BannerImageUrl,
            rewardItem.Category.Name,
            rewardItem.CostInPoints,
            rewardItem.MonetaryValue,
            rewardItem.HowToRedeem,
            rewardItem.TermsOfUse,
            rewardItem.IsActive
        );

        return Result<RewardProfileResponseDto>.Success(new RewardProfileResponseDto(rewardProfile, kpiStats));
    }
}