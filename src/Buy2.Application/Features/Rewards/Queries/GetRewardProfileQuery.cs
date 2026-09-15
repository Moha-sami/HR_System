using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Buy2.Application.Features.Rewards.Queries;

public record GetRewardProfileQuery(int Id) : IRequest<RewardProfileResponseDto>;
public class GetRewardProfileQueryHandler : IRequestHandler<GetRewardProfileQuery, RewardProfileResponseDto> {
    private readonly IRepository<RewardItem>        _rewardItemRepository;
    private readonly IRepository<RewardRedemption>  _redempationRepository;
    private readonly IRepository<RewardVoucher>     _voucherRepository;
    public GetRewardProfileQueryHandler(IRepository<RewardItem> item, IRepository<RewardRedemption> redempation, IRepository<RewardVoucher> voucher)
    {
        _rewardItemRepository   = item;
        _redempationRepository  = redempation;   
        _voucherRepository      = voucher;
    }
    public async Task<RewardProfileResponseDto> Handle(GetRewardProfileQuery query, CancellationToken cancellation)
    {
        var rewardItem = await _rewardItemRepository
            .Query(false)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellation);

        if (rewardItem is null) {
            throw new ValidationException("Reward item not found");
        }

        var redempation = await _redempationRepository
            .Query(false)
            .CountAsync(r => r.RewardItemId == query.Id, cancellation);

        var totalVoucher = await _voucherRepository
            .Query(false)
            .CountAsync(v => v.RewardItemId == query.Id, cancellation);
        var availableVoucher = await _voucherRepository
            .Query(false)
            .CountAsync(v => v.RewardItemId == query.Id &&
                             v.Status == VoucherStatus.Available,
                             cancellation);
        var availabilityStock =
            $"{availableVoucher}/{totalVoucher}";

        var totalCost = await _redempationRepository
            .Query(false)
            .Where(r => r.RewardItemId == query.Id)
            .Select(r => r.PointsTransaction)
                .SumAsync(p => (decimal)p.Amount, cancellation);

        var topRedeem = await _redempationRepository
            .Query(false)
            .Where(r =>
                r.RewardItemId == query.Id &&
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
                RedemptionCount : redempation,
                AvailableStock  : availabilityStock,
                TotalCost       : totalCost,
                TopRedeemed     : topRedeemedValue,
                PointsValue     : rewardItem.CostInPoints
            );

        var rewardProfile =  new RewardProfileListDto(
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
        return new RewardProfileResponseDto(rewardProfile, kpiStats);
    }
}