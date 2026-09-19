using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Api.Controllers;
using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Application.Features.Rewards.Queries;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Rewards;

public class GetRewardVoucherInventoryQueryTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task Handle_RewardNotFound_ReturnsNotFoundResult()
    {
        using var context = CreateDbContext();

        var handler = new GetRewardVoucherInventoryQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context)
        );

        var result = await handler.Handle(new GetRewardVoucherInventoryQuery(999, new VoucherInventoryFilterQueryDto()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
        Assert.Contains("Reward item not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_DefaultFilter_ReturnsAllVouchersAndComputedCounters()
    {
        using var context = CreateDbContext();

        var rewardItem = new RewardItem { Id = 1, RewardName = "Coffee Voucher", CostInPoints = 50, IsActive = true };
        context.RewardItems.Add(rewardItem);

        var now = DateTime.UtcNow;
        context.RewardVouchers.AddRange(
            new RewardVoucher { Id = 1, RewardItemId = 1, BatchId = 101, Code = "COFFEE-01", Status = VoucherStatus.Available, CreatedAt = now.AddDays(-5) },
            new RewardVoucher { Id = 2, RewardItemId = 1, BatchId = 101, Code = "COFFEE-02", Status = VoucherStatus.Available, CreatedAt = now.AddDays(-4) },
            new RewardVoucher { Id = 3, RewardItemId = 1, BatchId = 102, Code = "COFFEE-03", Status = VoucherStatus.Redeemed, CreatedAt = now.AddDays(-3) },
            new RewardVoucher { Id = 4, RewardItemId = 1, BatchId = 102, Code = "COFFEE-04", Status = VoucherStatus.Expired, CreatedAt = now.AddDays(-2) }
        );
        await context.SaveChangesAsync();

        var handler = new GetRewardVoucherInventoryQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context)
        );

        var result = await handler.Handle(new GetRewardVoucherInventoryQuery(1, new VoucherInventoryFilterQueryDto()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var response = result.Value;
        Assert.Equal(2, response.AvailableCount);
        Assert.Equal(1, response.RedeemedCount);
        Assert.Equal(1, response.ExpiredCount);

        Assert.Equal(4, response.Vouchers.TotalCount);
        Assert.Equal(1, response.Vouchers.Page);
        Assert.Equal(10, response.Vouchers.PageSize);
        Assert.Equal(4, response.Vouchers.Items.Count);

        // Verify descending order by CreatedAt
        var firstItem = response.Vouchers.Items.First();
        Assert.Equal("COFFEE-04", firstItem.VoucherCode);
        Assert.Equal(VoucherStatus.Expired, firstItem.Status);
    }

    [Fact]
    public async Task Handle_BatchIdFilter_FiltersByBatch()
    {
        using var context = CreateDbContext();

        var rewardItem = new RewardItem { Id = 2, RewardName = "Gift Card", CostInPoints = 100, IsActive = true };
        context.RewardItems.Add(rewardItem);

        context.RewardVouchers.AddRange(
            new RewardVoucher { Id = 10, RewardItemId = 2, BatchId = 500, Code = "CARD-A", Status = VoucherStatus.Available },
            new RewardVoucher { Id = 11, RewardItemId = 2, BatchId = 500, Code = "CARD-B", Status = VoucherStatus.Available },
            new RewardVoucher { Id = 12, RewardItemId = 2, BatchId = 600, Code = "CARD-C", Status = VoucherStatus.Available }
        );
        await context.SaveChangesAsync();

        var handler = new GetRewardVoucherInventoryQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context)
        );

        var result = await handler.Handle(new GetRewardVoucherInventoryQuery(2, new VoucherInventoryFilterQueryDto(BatchId: 500)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Vouchers.TotalCount);
        Assert.All(result.Value.Vouchers.Items, v => Assert.Equal(500, v.BatchId));
        Assert.Equal(3, result.Value.AvailableCount); // Total inventory counters preserved
    }

    [Fact]
    public async Task Handle_VoucherCodeFilter_FiltersBySubstring()
    {
        using var context = CreateDbContext();

        var rewardItem = new RewardItem { Id = 3, RewardName = "Meal Voucher", CostInPoints = 75, IsActive = true };
        context.RewardItems.Add(rewardItem);

        context.RewardVouchers.AddRange(
            new RewardVoucher { Id = 20, RewardItemId = 3, BatchId = 1, Code = "BURGER-ALPHA-99", Status = VoucherStatus.Available },
            new RewardVoucher { Id = 21, RewardItemId = 3, BatchId = 1, Code = "PIZZA-BETA-88", Status = VoucherStatus.Available },
            new RewardVoucher { Id = 22, RewardItemId = 3, BatchId = 1, Code = "BURGER-GAMMA-77", Status = VoucherStatus.Redeemed }
        );
        await context.SaveChangesAsync();

        var handler = new GetRewardVoucherInventoryQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context)
        );

        var result = await handler.Handle(new GetRewardVoucherInventoryQuery(3, new VoucherInventoryFilterQueryDto(VoucherCode: "BURGER")), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Vouchers.TotalCount);
        Assert.All(result.Value.Vouchers.Items, v => Assert.Contains("BURGER", v.VoucherCode));
    }

    [Fact]
    public async Task Handle_StatusFilter_FiltersByVoucherStatusEnum()
    {
        using var context = CreateDbContext();

        var rewardItem = new RewardItem { Id = 4, RewardName = "Gym Pass", CostInPoints = 200, IsActive = true };
        context.RewardItems.Add(rewardItem);

        context.RewardVouchers.AddRange(
            new RewardVoucher { Id = 30, RewardItemId = 4, BatchId = 1, Code = "GYM-1", Status = VoucherStatus.Available },
            new RewardVoucher { Id = 31, RewardItemId = 4, BatchId = 1, Code = "GYM-2", Status = VoucherStatus.Redeemed },
            new RewardVoucher { Id = 32, RewardItemId = 4, BatchId = 1, Code = "GYM-3", Status = VoucherStatus.Expired }
        );
        await context.SaveChangesAsync();

        var handler = new GetRewardVoucherInventoryQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context)
        );

        // Filter: Available
        var availResult = await handler.Handle(new GetRewardVoucherInventoryQuery(4, new VoucherInventoryFilterQueryDto(Status: "Available")), CancellationToken.None);
        Assert.True(availResult.IsSuccess);
        Assert.Single(availResult.Value!.Vouchers.Items);
        Assert.Equal(VoucherStatus.Available, availResult.Value.Vouchers.Items.First().Status);

        // Filter: Redeemed
        var redResult = await handler.Handle(new GetRewardVoucherInventoryQuery(4, new VoucherInventoryFilterQueryDto(Status: "Redeemed")), CancellationToken.None);
        Assert.True(redResult.IsSuccess);
        Assert.Single(redResult.Value!.Vouchers.Items);
        Assert.Equal(VoucherStatus.Redeemed, redResult.Value.Vouchers.Items.First().Status);

        // Filter: Expired
        var expResult = await handler.Handle(new GetRewardVoucherInventoryQuery(4, new VoucherInventoryFilterQueryDto(Status: "Expired")), CancellationToken.None);
        Assert.True(expResult.IsSuccess);
        Assert.Single(expResult.Value!.Vouchers.Items);
        Assert.Equal(VoucherStatus.Expired, expResult.Value.Vouchers.Items.First().Status);
    }

    [Fact]
    public async Task Handle_DateRangeFilter_FiltersByCreatedAtAndNormalizesInvertedBounds()
    {
        using var context = CreateDbContext();

        var rewardItem = new RewardItem { Id = 5, RewardName = "Book Store", CostInPoints = 60, IsActive = true };
        context.RewardItems.Add(rewardItem);

        context.RewardVouchers.AddRange(
            new RewardVoucher { Id = 40, RewardItemId = 5, BatchId = 1, Code = "BOOK-MAY", Status = VoucherStatus.Available, CreatedAt = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc) },
            new RewardVoucher { Id = 41, RewardItemId = 5, BatchId = 1, Code = "BOOK-JUNE", Status = VoucherStatus.Available, CreatedAt = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc) },
            new RewardVoucher { Id = 42, RewardItemId = 5, BatchId = 1, Code = "BOOK-JULY", Status = VoucherStatus.Available, CreatedAt = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc) }
        );
        await context.SaveChangesAsync();

        var handler = new GetRewardVoucherInventoryQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context)
        );

        // Inverted bounds (DateFrom > DateTo) should be normalized
        var invertedFilter = new VoucherInventoryFilterQueryDto(
            DateFrom: new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero),
            DateTo: new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero)
        );

        var result = await handler.Handle(new GetRewardVoucherInventoryQuery(5, invertedFilter), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Vouchers.TotalCount); // May and June
    }

    [Fact]
    public async Task Handle_NullFilter_DefaultsSafelyWithoutException()
    {
        using var context = CreateDbContext();

        var rewardItem = new RewardItem { Id = 6, RewardName = "Test Reward", CostInPoints = 20, IsActive = true };
        context.RewardItems.Add(rewardItem);
        await context.SaveChangesAsync();

        var handler = new GetRewardVoucherInventoryQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context)
        );

        var result = await handler.Handle(new GetRewardVoucherInventoryQuery(6, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(0, result.Value.Vouchers.TotalCount);
        Assert.Equal(0, result.Value.AvailableCount);
    }

    [Fact]
    public async Task Controller_GetInventory_MapsNotFoundAndOkCorrectly()
    {
        var dummyPage = new PageResultDto<RewardInventoryListDto>(new List<RewardInventoryListDto>(), 0, 1, 10);
        var dummyDto = new PaginatedVouchersResponseDto(dummyPage, 0, 0, 0);

        var okMediator = new FakeMediator(Result<PaginatedVouchersResponseDto>.Success(dummyDto));
        var okController = new RewardsController(okMediator);

        var okResult = await okController.GetInventory(1, new VoucherInventoryFilterQueryDto(), CancellationToken.None);
        var okObj = Assert.IsType<OkObjectResult>(okResult);
        Assert.Equal(StatusCodes.Status200OK, okObj.StatusCode);
        Assert.Equal(dummyDto, okObj.Value);

        var notFoundMediator = new FakeMediator(Result<PaginatedVouchersResponseDto>.NotFound("Reward item not found."));
        var notFoundController = new RewardsController(notFoundMediator);

        var notFoundResult = await notFoundController.GetInventory(999, new VoucherInventoryFilterQueryDto(), CancellationToken.None);
        var notFoundObj = Assert.IsType<NotFoundObjectResult>(notFoundResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundObj.StatusCode);
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

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
