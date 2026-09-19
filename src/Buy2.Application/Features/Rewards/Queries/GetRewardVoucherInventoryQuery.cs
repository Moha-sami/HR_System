using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Rewards.Queries;

public record GetRewardVoucherInventoryQuery(
    int Id,
    VoucherInventoryFilterQueryDto? Dto = null
) : IRequest<Result<PaginatedVouchersResponseDto>>;

public class GetRewardVoucherInventoryQueryHandler : IRequestHandler<GetRewardVoucherInventoryQuery, Result<PaginatedVouchersResponseDto>>
{
    private readonly IRepository<RewardItem> _rewardItemRepository;
    private readonly IRepository<RewardVoucher> _voucherRepository;

    public GetRewardVoucherInventoryQueryHandler(IRepository<RewardItem> item, IRepository<RewardVoucher> voucher)
    {
        _rewardItemRepository = item;
        _voucherRepository = voucher;
    }

    public async Task<Result<PaginatedVouchersResponseDto>> Handle(GetRewardVoucherInventoryQuery query, CancellationToken cancellation)
    {
        var rewardExists = await _rewardItemRepository
            .Query(true)
            .AnyAsync(r => r.Id == query.Id, cancellation);

        if (!rewardExists)
        {
            return Result<PaginatedVouchersResponseDto>.NotFound("Reward item not found.");
        }

        var filter = query.Dto ?? new VoucherInventoryFilterQueryDto();

        var baseQuery = _voucherRepository
            .Query(true)
            .Where(v => v.RewardItemId == query.Id);

        var availableCount = await baseQuery.CountAsync(v => v.Status == VoucherStatus.Available, cancellation);
        var redeemedCount = await baseQuery.CountAsync(v => v.Status == VoucherStatus.Redeemed, cancellation);
        var expiredCount = await baseQuery.CountAsync(v => v.Status == VoucherStatus.Expired, cancellation);

        var vouchers = baseQuery;

        if (filter.BatchId.HasValue)
        {
            vouchers = vouchers.Where(v => v.BatchId == filter.BatchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.VoucherCode))
        {
            var codeSearch = filter.VoucherCode.Trim();
            vouchers = vouchers.Where(v => v.Code.Contains(codeSearch));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && !string.Equals(filter.Status, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<VoucherStatus>(filter.Status.Trim(), true, out var parsedStatus))
            {
                vouchers = vouchers.Where(v => v.Status == parsedStatus);
            }
        }

        var dateFrom = filter.DateFrom;
        var dateTo = filter.DateTo;
        if (dateFrom.HasValue && dateTo.HasValue && dateFrom > dateTo)
        {
            (dateFrom, dateTo) = (dateTo, dateFrom);
        }

        if (dateFrom.HasValue)
        {
            var fromUtc = dateFrom.Value.UtcDateTime;
            vouchers = vouchers.Where(v => v.CreatedAt >= fromUtc);
        }

        if (dateTo.HasValue)
        {
            var toUtc = dateTo.Value.UtcDateTime;
            vouchers = vouchers.Where(v => v.CreatedAt <= toUtc);
        }

        var totalCount = await vouchers.CountAsync(cancellation);

        var page = Math.Max(1, filter.Page);
        var pageSize = filter.PageSize < 1 ? 10 : Math.Min(filter.PageSize, 100);

        var items = await vouchers
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new RewardInventoryListDto(
                v.Id,
                v.BatchId,
                v.CreatedAt,
                v.Code,
                v.Status
            ))
            .ToListAsync(cancellation);

        var pageResult = new PageResultDto<RewardInventoryListDto>(
            items,
            totalCount,
            page,
            pageSize);

        return Result<PaginatedVouchersResponseDto>.Success(new PaginatedVouchersResponseDto(
            pageResult,
            availableCount,
            redeemedCount,
            expiredCount));
    }
}