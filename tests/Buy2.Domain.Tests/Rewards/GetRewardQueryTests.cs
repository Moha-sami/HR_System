using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Application.Features.Rewards.Queries;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Rewards;

public class GetRewardQueryTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static async Task SeedRewardsAsync(Buy2DbContext context)
    {
        var catGiftCards = new RewardCategory { Id = 1, Name = "Gift Cards" };
        var catTech = new RewardCategory { Id = 2, Name = "Technology" };

        context.RewardCategories.AddRange(catGiftCards, catTech);

        var reward1 = new RewardItem
        {
            Id = 1,
            RewardName = "Amazon 50 USD Voucher",
            CategoryId = 1,
            Category = catGiftCards,
            CostInPoints = 500,
            MonetaryValue = 50.0m,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Vouchers = new List<RewardVoucher>
            {
                new RewardVoucher { Id = 1, Code = "AMZ-01", Status = VoucherStatus.Available },
                new RewardVoucher { Id = 2, Code = "AMZ-02", Status = VoucherStatus.Available },
                new RewardVoucher { Id = 3, Code = "AMZ-03", Status = VoucherStatus.Redeemed }
            },
            Redemptions = new List<RewardRedemption>
            {
                new RewardRedemption { Id = 1, VoucherCode = "AMZ-03" }
            }
        };

        var reward2 = new RewardItem
        {
            Id = 2,
            RewardName = "Apple AirPods",
            CategoryId = 2,
            Category = catTech,
            CostInPoints = 1500,
            MonetaryValue = 150.0m,
            IsActive = true,
            CreatedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            Vouchers = new List<RewardVoucher>
            {
                new RewardVoucher { Id = 4, Code = "APP-01", Status = VoucherStatus.Available }
            },
            Redemptions = new List<RewardRedemption>
            {
                new RewardRedemption { Id = 2, VoucherCode = "APP-RED-1" },
                new RewardRedemption { Id = 3, VoucherCode = "APP-RED-2" },
                new RewardRedemption { Id = 4, VoucherCode = "APP-RED-3" }
            }
        };

        var reward3 = new RewardItem
        {
            Id = 3,
            RewardName = "Netflix Subscription (Inactive)",
            CategoryId = 1,
            Category = catGiftCards,
            CostInPoints = 300,
            MonetaryValue = 30.0m,
            IsActive = false,
            CreatedAt = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc),
            Vouchers = new List<RewardVoucher>(),
            Redemptions = new List<RewardRedemption>()
        };

        context.RewardItems.AddRange(reward1, reward2, reward3);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetRewards_ReturnsAllActiveAndInactiveByDefault_WithCorrectStockRatio()
    {
        using var context = CreateDbContext();
        await SeedRewardsAsync(context);

        var repo = new GenericRepository<RewardItem>(context);
        var handler = new GetRewardQueryHandler(repo);

        var query = new GetRewardQuery(
            Search: null,
            Status: null,
            FromDate: null,
            ToDate: null,
            SortBy: null
        );

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);

        var amazon = result.Items.First(r => r.Id == 1);
        Assert.Equal("Amazon 50 USD Voucher", amazon.Name);
        Assert.Equal("Gift Cards", amazon.Category);
        Assert.Equal(500, amazon.Points);
        Assert.Equal(50.0m, amazon.MonetaryValue);
        Assert.Equal("2/3", amazon.StockRatio);
        Assert.Equal(1, amazon.RedemptionCount);
        Assert.True(amazon.IsActive);
    }

    [Fact]
    public async Task GetRewards_FilterSearch_MatchesRewardNameOrCategory()
    {
        using var context = CreateDbContext();
        await SeedRewardsAsync(context);

        var repo = new GenericRepository<RewardItem>(context);
        var handler = new GetRewardQueryHandler(repo);

        var searchNameQuery = new GetRewardQuery(Search: "AirPods", Status: null, FromDate: null, ToDate: null, SortBy: null);
        var searchNameResult = await handler.Handle(searchNameQuery, CancellationToken.None);
        Assert.Single(searchNameResult.Items);
        Assert.Equal("Apple AirPods", searchNameResult.Items.First().Name);

        var searchCatQuery = new GetRewardQuery(Search: "Technology", Status: null, FromDate: null, ToDate: null, SortBy: null);
        var searchCatResult = await handler.Handle(searchCatQuery, CancellationToken.None);
        Assert.Single(searchCatResult.Items);
        Assert.Equal("Technology", searchCatResult.Items.First().Category);
    }

    [Theory]
    [InlineData("Active", 2)]
    [InlineData("Inactive", 1)]
    [InlineData("All", 3)]
    public async Task GetRewards_FilterStatus_ReturnsCorrectCount(string status, int expectedCount)
    {
        using var context = CreateDbContext();
        await SeedRewardsAsync(context);

        var repo = new GenericRepository<RewardItem>(context);
        var handler = new GetRewardQueryHandler(repo);

        var query = new GetRewardQuery(Search: null, Status: status, FromDate: null, ToDate: null, SortBy: null);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(expectedCount, result.TotalCount);
        Assert.Equal(expectedCount, result.Items.Count);
    }

    [Fact]
    public async Task GetRewards_SortBy_PointsAndCost_AscendingAndDescending()
    {
        using var context = CreateDbContext();
        await SeedRewardsAsync(context);

        var repo = new GenericRepository<RewardItem>(context);
        var handler = new GetRewardQueryHandler(repo);

        var queryPointsDesc = new GetRewardQuery(Search: null, Status: null, FromDate: null, ToDate: null, SortBy: "points", SortDescending: true);
        var resultPointsDesc = await handler.Handle(queryPointsDesc, CancellationToken.None);
        Assert.Equal(1500, resultPointsDesc.Items.First().Points);
        Assert.Equal(300, resultPointsDesc.Items.Last().Points);

        var queryCostAsc = new GetRewardQuery(Search: null, Status: null, FromDate: null, ToDate: null, SortBy: "cost", SortDescending: false);
        var resultCostAsc = await handler.Handle(queryCostAsc, CancellationToken.None);
        Assert.Equal(300, resultCostAsc.Items.First().Points);
        Assert.Equal(1500, resultCostAsc.Items.Last().Points);
    }

    [Fact]
    public async Task GetRewards_SortBy_Price_AscendingAndDescending()
    {
        using var context = CreateDbContext();
        await SeedRewardsAsync(context);

        var repo = new GenericRepository<RewardItem>(context);
        var handler = new GetRewardQueryHandler(repo);

        var queryPriceDesc = new GetRewardQuery(Search: null, Status: null, FromDate: null, ToDate: null, SortBy: "price", SortDescending: true);
        var resultPriceDesc = await handler.Handle(queryPriceDesc, CancellationToken.None);
        Assert.Equal(150.0m, resultPriceDesc.Items.First().MonetaryValue);
        Assert.Equal(30.0m, resultPriceDesc.Items.Last().MonetaryValue);
    }

    [Fact]
    public async Task GetRewards_SortBy_RedemptionCount_Descending()
    {
        using var context = CreateDbContext();
        await SeedRewardsAsync(context);

        var repo = new GenericRepository<RewardItem>(context);
        var handler = new GetRewardQueryHandler(repo);

        var queryRedemptionsDesc = new GetRewardQuery(Search: null, Status: null, FromDate: null, ToDate: null, SortBy: "redemptioncount", SortDescending: true);
        var resultRedemptionsDesc = await handler.Handle(queryRedemptionsDesc, CancellationToken.None);
        Assert.Equal(3, resultRedemptionsDesc.Items.First().RedemptionCount);
        Assert.Equal(0, resultRedemptionsDesc.Items.Last().RedemptionCount);
    }

    [Fact]
    public async Task GetRewards_FilterDateRange_FiltersCorrectly()
    {
        using var context = CreateDbContext();
        await SeedRewardsAsync(context);

        var repo = new GenericRepository<RewardItem>(context);
        var handler = new GetRewardQueryHandler(repo);

        var from = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 2, 28, 0, 0, 0, TimeSpan.Zero);

        var query = new GetRewardQuery(Search: null, Status: null, FromDate: from, ToDate: to, SortBy: null);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Apple AirPods", result.Items.First().Name);
    }

    [Fact]
    public async Task GetRewards_Pagination_WorksAccurately()
    {
        using var context = CreateDbContext();
        await SeedRewardsAsync(context);

        var repo = new GenericRepository<RewardItem>(context);
        var handler = new GetRewardQueryHandler(repo);

        var page1Query = new GetRewardQuery(Search: null, Status: null, FromDate: null, ToDate: null, SortBy: "name", SortDescending: false, Page: 1, PageSize: 2);
        var page1 = await handler.Handle(page1Query, CancellationToken.None);

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(1, page1.Page);
        Assert.Equal(2, page1.PageSize);

        var page2Query = new GetRewardQuery(Search: null, Status: null, FromDate: null, ToDate: null, SortBy: "name", SortDescending: false, Page: 2, PageSize: 2);
        var page2 = await handler.Handle(page2Query, CancellationToken.None);

        Assert.Equal(3, page2.TotalCount);
        Assert.Single(page2.Items);
    }

    [Fact]
    public async Task RewardsController_GetRewards_ReturnsOkResult()
    {
        var expectedResult = new PageResultDto<RewardListDto>(
            Items: new List<RewardListDto>
            {
                new RewardListDto(1, "Test Reward", "Category", 100, 10m, "5/10", 2, true)
            },
            TotalCount: 1,
            Page: 1,
            PageSize: 10
        );

        var fakeMediator = new FakeMediator(expectedResult);
        var controller = new RewardsController(fakeMediator);

        var query = new GetRewardQuery(null, null, null, null, null);
        var actionResult = await controller.GetRewards(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expectedResult, okResult.Value);
    }

    private class FakeMediator : ISender
    {
        private readonly object _response;
        public FakeMediator(object response) => _response = response;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => Task.FromResult((TResponse)_response);

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => Task.FromResult<object?>(_response);

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public IAsyncEnumerable<TResponse> CreateStream<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IStreamRequest<TResponse>
            => throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
