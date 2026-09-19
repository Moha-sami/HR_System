using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Buy2.Application.Features.Rewards.Queries;
public record GetRewardVoucherInventoryQuery(
    int Id,
    VoucherInventoryFilterQueryDto dto
) : IRequest<PaginatedVouchersResponseDto>;

public class GetRewardVoucherInventoryQueryHandler : IRequestHandler<GetRewardVoucherInventoryQuery, PaginatedVouchersResponseDto>
{
    private readonly IRepository<RewardItem> _rewardItemRepository;
    private readonly IRepository<RewardVoucher> _voucherRepository;
    public GetRewardVoucherInventoryQueryHandler(IRepository<RewardItem> item, IRepository<RewardVoucher> voucher)
    {
        _rewardItemRepository   = item;
        _voucherRepository      = voucher;
    }
    public async Task<PaginatedVouchersResponseDto> Handle(GetRewardVoucherInventoryQuery query, CancellationToken cancellation)
    {
        var rewardItem = await _rewardItemRepository
            .Query(false)
            .AnyAsync(r => r.Id == query.Id, cancellation);

        if (!rewardItem)
        {
            throw new ValidationException("Reward item not found.");
        }

        var vouchers = _voucherRepository
            .Query(true)
            .Where(v => v.RewardItemId == query.Id);

        if (query.dto.BatchId.HasValue)
        {
            vouchers = vouchers.Where(v => v.BatchId == query.dto.BatchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.dto.Status) && !query.dto.Status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (query.dto.Status.Equals("Available", StringComparison.OrdinalIgnoreCase))
            {
                vouchers = vouchers.Where(v => v.Status.Equals("Available"));
            }
            else if (query.dto.Status.Equals("Redeemed", StringComparison.OrdinalIgnoreCase))
            {
                vouchers = vouchers.Where(v => v.Status.Equals("Redeemed"));
            }
            else if (query.dto.Status.Equals("Expired", StringComparison.OrdinalIgnoreCase))
            {
                vouchers = vouchers.Where(v => v.Status.Equals("Expired"));
            }
        }

        if (query.dto.DateFrom.HasValue)
        {
            vouchers = vouchers.Where(r => r.CreatedAt >= query.dto.DateFrom);
        }
        if (query.dto.DateTo.HasValue)
        {
            vouchers = vouchers.Where(r => r.CreatedAt <= query.dto.DateTo);
        }

        var totalCount = await vouchers
           .CountAsync(cancellation);

        var availableCount = await vouchers
            .CountAsync(
                v => v.Status == VoucherStatus.Available,
                cancellation);

        var redeemedCount = await vouchers
            .CountAsync(
                v => v.Status == VoucherStatus.Redeemed,
                cancellation);

        var expiredCount = await vouchers
            .CountAsync(
                v => v.Status == VoucherStatus.Expired,
                cancellation);
        var page = query.dto.Page < 1 ? 1 : query.dto.Page;
        var pageSize = query.dto.PageSize < 1 ? 10 : query.dto.PageSize;

        var items = await vouchers
            .Skip((page - 1) * pageSize)
            .Take(query.dto.PageSize)
            .Select(v => new RewardInventoryListDto(
                    v.Id,
                    v.BatchId,
                    v.CreatedAt,
                    v.Code,
                    v.Status
                )         
            ).ToListAsync(cancellation);

        var pageResult = new PageResultDto<RewardInventoryListDto>(
            items,
            totalCount,
            page,
            pageSize);

        return new PaginatedVouchersResponseDto(
            pageResult,
            availableCount,
            redeemedCount,
            expiredCount);
    }
}