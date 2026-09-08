using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Rewards.Queries;

public record GetRewardQuery(
    string? Search,
    string? Status,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    string? SortBy,
    bool SortDescending = false,
    int Page = 1,
    int PageSize = 10
    ) : IRequest<PageResultDto<RewardListDto>>;
public class GetRewardQueryHandler : IRequestHandler<GetRewardQuery, PageResultDto<RewardListDto>>
{
    private readonly IRepository<RewardItem> _rewardItemRepository;
    public GetRewardQueryHandler(IRepository<RewardItem> rewardItemRepository)
    {
        _rewardItemRepository = rewardItemRepository;
    }
    public async Task<PageResultDto<RewardListDto>> Handle(GetRewardQuery query, CancellationToken cancellation)
    {
        var rewardQuery = _rewardItemRepository.Query(false);

        // Search By Reward Name Or Category Name
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rewardQuery = rewardQuery.Where(
                r => r.RewardName.Contains(search) ||
                    r.Category.Name.Contains(search)
                );
        }

        // Filter Status
        if (!string.IsNullOrWhiteSpace(query.Status) && 
            !query.Status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (query.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                rewardQuery = rewardQuery.Where(r => r.IsActive);
            }
            else if(query.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
            {
                rewardQuery = rewardQuery.Where(r => !r.IsActive);
            }
        }

        // Filter Sorted
        rewardQuery = query.SortBy?.ToLowerInvariant() switch
        {
            "name" => query.SortDescending
                ? rewardQuery.OrderByDescending(r => r.RewardName)
                : rewardQuery.OrderBy(r => r.RewardName),
            "cost" or "points" => query.SortDescending
                ? rewardQuery.OrderByDescending(r => r.CostInPoints)
                : rewardQuery.OrderBy(r => r.CostInPoints),
            "price" => query.SortDescending
                ? rewardQuery.OrderByDescending(r => r.MonetaryValue)
                : rewardQuery.OrderBy(r => r.MonetaryValue),
            "redemptioncount" => query.SortDescending
                ? rewardQuery.OrderByDescending(r => r.Redemptions.Count)
                : rewardQuery.OrderBy(r => r.Redemptions.Count),
            _ => rewardQuery.OrderBy(r => r.RewardName)
        };

        // Custom Date
        if (query.FromDate.HasValue)
        {
            rewardQuery = rewardQuery.Where(r => r.CreatedAt >= query.FromDate);
        }
        if (query.ToDate.HasValue)
        {
            rewardQuery = rewardQuery.Where(r => r.CreatedAt <= query.ToDate);
        }

        var totalCount = await rewardQuery.CountAsync(cancellation);

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        // Pagination
        var reward = await rewardQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RewardListDto(
                    r.Id,
                    r.RewardName,
                    r.Category.Name,
                    r.CostInPoints,
                    r.MonetaryValue,
                    r.Vouchers.Count(v => v.Status == VoucherStatus.Available)
                    +"/"+ r.Vouchers.Count(),
                    r.Redemptions.Count,
                    r.IsActive
                )).ToListAsync(cancellation);

        return new PageResultDto<RewardListDto>
            (
                reward,
                totalCount,
                page,
                pageSize
            );
    }
}