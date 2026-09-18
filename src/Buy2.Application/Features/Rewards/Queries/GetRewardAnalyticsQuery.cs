using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Rewards.Queries;

public record GetRewardAnalyticsQuery(
    int Id,
    RewardTransactionFilterQueryDto? Filter = null
) : IRequest<Result<RewardAnalyticsDto>>;

public class GetRewardAnalyticsQueryHandler : IRequestHandler<GetRewardAnalyticsQuery, Result<RewardAnalyticsDto>>
{
    private readonly IRepository<RewardItem> _rewardItemRepository;
    private readonly IRepository<RewardRedemption> _redemptionRepository;

    public GetRewardAnalyticsQueryHandler(
        IRepository<RewardItem> rewardItemRepository,
        IRepository<RewardRedemption> redemptionRepository)
    {
        _rewardItemRepository = rewardItemRepository;
        _redemptionRepository = redemptionRepository;
    }

    public async Task<Result<RewardAnalyticsDto>> Handle(GetRewardAnalyticsQuery query, CancellationToken cancellation)
    {
        var rewardItem = await _rewardItemRepository
            .Query(true)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellation);

        if (rewardItem is null)
        {
            return Result<RewardAnalyticsDto>.NotFound("Reward item not found.");
        }

        var filter = query.Filter ?? new RewardTransactionFilterQueryDto();

        var (dateFrom, dateTo, isWeekly) = ResolveDateBounds(filter);

        var timeline = await BuildTimelineAsync(query.Id, dateFrom, dateTo, isWeekly, cancellation);

        var transactions = await BuildTransactionsAsync(query.Id, filter, dateFrom, dateTo, cancellation);

        var totalPages = transactions.TotalCount == 0
            ? 0
            : (int)Math.Ceiling((double)transactions.TotalCount / transactions.PageSize);

        return Result<RewardAnalyticsDto>.Success(new RewardAnalyticsDto(
            timeline,
            transactions.Items.ToList(),
            transactions.TotalCount,
            transactions.Page,
            transactions.PageSize,
            totalPages
        ));
    }

    private static (DateTimeOffset From, DateTimeOffset To, bool IsWeekly) ResolveDateBounds(RewardTransactionFilterQueryDto filter)
    {
        var to = filter.DateTo ?? DateTimeOffset.UtcNow;
        var from = filter.DateFrom ?? to.AddMonths(-6);
        if (from > to)
        {
            (from, to) = (to, from);
        }
        var isWeekly = string.Equals(filter.TimelinePeriod, "Weekly", StringComparison.OrdinalIgnoreCase);
        return (from, to, isWeekly);
    }

    private async Task<List<RedemptionTimelinePointDto>> BuildTimelineAsync(
        int rewardId, DateTimeOffset dateFrom, DateTimeOffset dateTo, bool isWeekly, CancellationToken cancellation)
    {
        var redemptions = await _redemptionRepository
            .Query(true)
            .Where(r =>
                r.RewardItemId == rewardId &&
                r.RedeemedAt >= dateFrom &&
                r.RedeemedAt <= dateTo)
            .Select(r => new
            {
                r.RedeemedAt,
                PointsSpent = r.PointsTransaction != null ? Math.Abs(r.PointsTransaction.Amount) : 0
            })
            .ToListAsync(cancellation);

        return isWeekly
            ? redemptions
                .GroupBy(r => StartOfWeek(r.RedeemedAt))
                .Select(g => new RedemptionTimelinePointDto(
                    g.Key.ToString("dd MMM yyyy"),
                    g.Key,
                    g.Key.AddDays(6),
                    g.Count(),
                    g.Sum(r => r.PointsSpent)))
                .OrderBy(p => p.DateFrom)
                .ToList()
            : redemptions
                .GroupBy(r => new DateTimeOffset(r.RedeemedAt.DateTime.Year, r.RedeemedAt.DateTime.Month, 1, 0, 0, 0, TimeSpan.Zero))
                .Select(g => new RedemptionTimelinePointDto(
                    g.Key.ToString("MMM yyyy"),
                    g.Key,
                    g.Key.AddMonths(1).AddDays(-1),
                    g.Count(),
                    g.Sum(r => r.PointsSpent)))
                .OrderBy(p => p.DateFrom)
                .ToList();
    }

    private async Task<PageResultDto<RewardTransactionItemDto>> BuildTransactionsAsync(
        int rewardId, RewardTransactionFilterQueryDto filter, DateTimeOffset dateFrom, DateTimeOffset dateTo, CancellationToken cancellation)
    {
        var redemptionQuery = _redemptionRepository
            .Query(true)
            .Include(r => r.Employee)
                .ThenInclude(e => e.JobRole)
                    .ThenInclude(j => j!.Department)
            .Include(r => r.PointsTransaction)
            .Where(r =>
                r.RewardItemId == rewardId &&
                r.RedeemedAt >= dateFrom &&
                r.RedeemedAt <= dateTo);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim();
            redemptionQuery = redemptionQuery.Where(r =>
                r.Employee.FirstName.Contains(search) ||
                r.Employee.LastName.Contains(search) ||
                r.Employee.EmployeeCode.Contains(search) ||
                r.VoucherCode.Contains(search));
        }

        var totalCount = await redemptionQuery.CountAsync(cancellation);

        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = filter.PageSize < 1 ? 10 : Math.Min(filter.PageSize, 100);

        var items = await redemptionQuery
            .OrderByDescending(r => r.RedeemedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RewardTransactionItemDto(
                r.Id,
                r.EmployeeId,
                r.Employee != null ? r.Employee.FirstName + " " + r.Employee.LastName : string.Empty,
                r.Employee != null ? r.Employee.EmployeeCode : string.Empty,
                r.Employee != null && r.Employee.JobRole != null && r.Employee.JobRole.Department != null
                    ? r.Employee.JobRole.Department.Name
                    : string.Empty,
                r.VoucherCode,
                r.RedeemedAt,
                r.RedeemedAt.DateTime.TimeOfDay,
                r.PointsTransaction != null ? Math.Abs(r.PointsTransaction.Amount) : 0
            ))
            .ToListAsync(cancellation);

        return new PageResultDto<RewardTransactionItemDto>(items, totalCount, pageNumber, pageSize);
    }

    private static DateTimeOffset StartOfWeek(DateTimeOffset date)
    {
        var day = date.DateTime.Date;
        var diff = (7 + (day.DayOfWeek - DayOfWeek.Monday)) % 7;
        return new DateTimeOffset(day.AddDays(-diff), TimeSpan.Zero);
    }
}