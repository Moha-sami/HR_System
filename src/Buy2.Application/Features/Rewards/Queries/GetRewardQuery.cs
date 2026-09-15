using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;

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
        var spec = new Specification<RewardItem>()
            .Include(nameof(RewardItem.Category), nameof(RewardItem.Vouchers), nameof(RewardItem.Redemptions));

        // Search By Reward Name Or Category Name
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            spec.Where(
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
                spec.Where(r => r.IsActive);
            }
            else if(query.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
            {
                spec.Where(r => !r.IsActive);
            }
        }

        // Filter Sorted
        switch (query.SortBy?.ToLowerInvariant())
        {
            case "name":
                spec.OrderBy(r => r.RewardName, descending: query.SortDescending);
                break;
            case "cost":
            case "points":
                spec.OrderBy(r => r.CostInPoints, descending: query.SortDescending);
                break;
            case "price":
                spec.OrderBy(r => r.MonetaryValue, descending: query.SortDescending);
                break;
            case "redemptioncount":
                spec.OrderBy(r => r.Redemptions.Count, descending: query.SortDescending);
                break;
            default:
                spec.OrderBy(r => r.RewardName);
                break;
        }

        // Custom Date
        if (query.FromDate.HasValue)
        {
            var fromDate = query.FromDate.Value;
            spec.Where(r => r.CreatedAt >= fromDate);
        }
        if (query.ToDate.HasValue)
        {
            var toDate = query.ToDate.Value;
            spec.Where(r => r.CreatedAt <= toDate);
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        var paged = await _rewardItemRepository.PagedAsync(spec, page, pageSize, cancellation);

        // Pagination
        var reward = paged.Items
            .Select(r => new RewardListDto(
                    r.Id,
                    r.RewardName,
                    r.Category != null ? r.Category.Name : string.Empty,
                    r.CostInPoints,
                    r.MonetaryValue,
                    (r.Vouchers != null ? r.Vouchers.Count(v => v.Status == VoucherStatus.Available) : 0)
                    +"/"+ (r.Vouchers != null ? r.Vouchers.Count() : 0),
                    r.Redemptions != null ? r.Redemptions.Count : 0,
                    r.IsActive
                )).ToList();

        return new PageResultDto<RewardListDto>
            (
                reward,
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize
            );
    }
}