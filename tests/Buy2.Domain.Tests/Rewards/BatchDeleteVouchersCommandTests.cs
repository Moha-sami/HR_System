using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Api.Controllers;
using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Application.Features.Rewards.Commands;
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

public class BatchDeleteVouchersCommandTests
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
    public async Task Handle_VoucherIdsNullOrEmpty_ReturnsValidationFailure()
    {
        using var context = CreateDbContext();
        var handler = new BatchDeleteVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new UnitOfWork(context)
        );

        var nullResult = await handler.Handle(new BatchDeleteVouchersCommand(1, null!), CancellationToken.None);
        Assert.False(nullResult.IsSuccess);
        Assert.Contains("At least one voucher must be selected", nullResult.ErrorMessage);

        var emptyResult = await handler.Handle(new BatchDeleteVouchersCommand(1, new List<int>()), CancellationToken.None);
        Assert.False(emptyResult.IsSuccess);
        Assert.Contains("At least one voucher must be selected", emptyResult.ErrorMessage);
    }

    [Fact]
    public async Task Handle_RewardItemNotFound_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        var handler = new BatchDeleteVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new UnitOfWork(context)
        );

        var result = await handler.Handle(new BatchDeleteVouchersCommand(999, new List<int> { 1 }), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
        Assert.Contains("Reward item not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_AllAvailableVouchers_DeletesThemAndDecrementsStock()
    {
        using var context = CreateDbContext();
        var rewardItem = new RewardItem { Id = 1, RewardName = "Test Reward", CostInPoints = 50, AvailableStock = 5, IsActive = true };
        context.RewardItems.Add(rewardItem);
        context.RewardVouchers.AddRange(
            new RewardVoucher { Id = 10, RewardItemId = 1, Code = "VOUCH-10", Status = VoucherStatus.Available },
            new RewardVoucher { Id = 11, RewardItemId = 1, Code = "VOUCH-11", Status = VoucherStatus.Available },
            new RewardVoucher { Id = 12, RewardItemId = 1, Code = "VOUCH-12", Status = VoucherStatus.Available }
        );
        await context.SaveChangesAsync();

        var handler = new BatchDeleteVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new UnitOfWork(context)
        );

        var command = new BatchDeleteVouchersCommand(1, new List<int> { 10, 11, 12 });
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.DeletedCount);
        Assert.Equal(0, result.Value.SkippedCount);
        Assert.Equal("Voucher codes deleted successfully.", result.Value.Message);

        // Verify stock decremented
        var updatedItem = await context.RewardItems.FindAsync(1);
        Assert.Equal(2, updatedItem!.AvailableStock);

        // Verify vouchers removed from DB
        var remainingVouchers = await context.RewardVouchers.Where(v => v.RewardItemId == 1).ToListAsync();
        Assert.Empty(remainingVouchers);
    }

    [Fact]
    public async Task Handle_MixedVouchers_OnlyDeletesAvailableAndSkipsNonAvailable()
    {
        using var context = CreateDbContext();
        var rewardItem = new RewardItem { Id = 1, RewardName = "Test Reward", CostInPoints = 50, AvailableStock = 10, IsActive = true };
        context.RewardItems.Add(rewardItem);
        context.RewardVouchers.AddRange(
            new RewardVoucher { Id = 1, RewardItemId = 1, Code = "V-AVAILABLE", Status = VoucherStatus.Available },
            new RewardVoucher { Id = 2, RewardItemId = 1, Code = "V-REDEEMED", Status = VoucherStatus.Redeemed },
            new RewardVoucher { Id = 3, RewardItemId = 1, Code = "V-EXPIRED", Status = VoucherStatus.Expired }
        );
        await context.SaveChangesAsync();

        var handler = new BatchDeleteVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new UnitOfWork(context)
        );

        // Pass available, redeemed, expired, and a non-existent ID 999
        var command = new BatchDeleteVouchersCommand(1, new List<int> { 1, 2, 3, 999 });
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.DeletedCount);
        Assert.Equal(3, result.Value.SkippedCount); // 2 non-available + 1 not found
        Assert.Equal("Cannot delete redeemed voucher codes. Only unused available vouchers can be removed from inventory.", result.Value.Message);

        // Stock decreased by 1
        var updatedItem = await context.RewardItems.FindAsync(1);
        Assert.Equal(9, updatedItem!.AvailableStock);

        // Available voucher removed, others remain
        var remainingVouchers = await context.RewardVouchers.Where(v => v.RewardItemId == 1).ToListAsync();
        Assert.Equal(2, remainingVouchers.Count);
        Assert.Contains(remainingVouchers, v => v.Code == "V-REDEEMED");
        Assert.Contains(remainingVouchers, v => v.Code == "V-EXPIRED");
    }

    [Fact]
    public async Task Handle_AllNonAvailableVouchers_DeletesNoneAndDoesNotChangeStock()
    {
        using var context = CreateDbContext();
        var rewardItem = new RewardItem { Id = 1, RewardName = "Test Reward", CostInPoints = 50, AvailableStock = 5, IsActive = true };
        context.RewardItems.Add(rewardItem);
        context.RewardVouchers.AddRange(
            new RewardVoucher { Id = 1, RewardItemId = 1, Code = "V-REDEEMED", Status = VoucherStatus.Redeemed },
            new RewardVoucher { Id = 2, RewardItemId = 1, Code = "V-EXPIRED", Status = VoucherStatus.Expired }
        );
        await context.SaveChangesAsync();

        var handler = new BatchDeleteVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new UnitOfWork(context)
        );

        var command = new BatchDeleteVouchersCommand(1, new List<int> { 1, 2 });
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(0, result.Value.DeletedCount);
        Assert.Equal(2, result.Value.SkippedCount);
        Assert.Equal("Cannot delete redeemed voucher codes. Only unused available vouchers can be removed from inventory.", result.Value.Message);

        var updatedItem = await context.RewardItems.FindAsync(1);
        Assert.Equal(5, updatedItem!.AvailableStock);

        var remainingVouchers = await context.RewardVouchers.Where(v => v.RewardItemId == 1).ToListAsync();
        Assert.Equal(2, remainingVouchers.Count);
    }

    [Fact]
    public async Task Handle_DuplicateIdsInRequest_DeduplicatesBeforeProcessing()
    {
        using var context = CreateDbContext();
        var rewardItem = new RewardItem { Id = 1, RewardName = "Test Reward", CostInPoints = 50, AvailableStock = 5, IsActive = true };
        context.RewardItems.Add(rewardItem);
        context.RewardVouchers.Add(
            new RewardVoucher { Id = 1, RewardItemId = 1, Code = "V-AVAILABLE", Status = VoucherStatus.Available }
        );
        await context.SaveChangesAsync();

        var handler = new BatchDeleteVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new UnitOfWork(context)
        );

        var command = new BatchDeleteVouchersCommand(1, new List<int> { 1, 1, 1 });
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.DeletedCount);
        Assert.Equal(0, result.Value.SkippedCount);
    }

    [Fact]
    public async Task Controller_BatchDeleteVouchers_Returns404NotFound()
    {
        var fakeMediator = new FakeMediator(Result<BatchDeleteVouchersResultDto>.NotFound("Reward item not found."));
        var controller = new RewardsController(fakeMediator);

        var actionResult = await controller.BatchDeleteVouchers(999, new List<int> { 1 }, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task Controller_BatchDeleteVouchers_Returns400BadRequest()
    {
        var fakeMediator = new FakeMediator(Result<BatchDeleteVouchersResultDto>.ValidationFailure("At least one voucher must be selected."));
        var controller = new RewardsController(fakeMediator);

        var actionResult = await controller.BatchDeleteVouchers(1, null, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Controller_BatchDeleteVouchers_Returns200OkOnSuccess()
    {
        var expectedDto = new BatchDeleteVouchersResultDto(2, 0, "Voucher codes deleted successfully.");
        var fakeMediator = new FakeMediator(Result<BatchDeleteVouchersResultDto>.Success(expectedDto));
        var controller = new RewardsController(fakeMediator);

        var actionResult = await controller.BatchDeleteVouchers(1, new List<int> { 1, 2 }, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(expectedDto, okResult.Value);
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
