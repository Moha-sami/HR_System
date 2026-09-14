using System;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Api.Controllers;
using Buy2.Application.Common.Models;
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

public class DeleteRewardCommandTests
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

        var handler = new DeleteRewardCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new GenericRepository<RewardRedemption>(context),
            new UnitOfWork(context)
        );

        var result = await handler.Handle(new DeleteRewardCommand(999), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
        Assert.Contains("Reward item not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_RewardHasAvailableVouchers_ReturnsConflictResult()
    {
        using var context = CreateDbContext();
        var item = new RewardItem
        {
            Id = 1,
            RewardName = "Coffee Voucher",
            CostInPoints = 50,
            AvailableStock = 0,
            IsActive = true
        };
        context.RewardItems.Add(item);

        context.RewardVouchers.Add(new RewardVoucher
        {
            Id = 101,
            RewardItemId = 1,
            Code = "CODE123",
            Status = VoucherStatus.Available
        });
        await context.SaveChangesAsync();

        var handler = new DeleteRewardCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new GenericRepository<RewardRedemption>(context),
            new UnitOfWork(context)
        );

        var result = await handler.Handle(new DeleteRewardCommand(1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsConflict);
        Assert.Contains("active voucher inventory", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_RewardHasRedemptions_SoftDeactivatesReward()
    {
        using var context = CreateDbContext();
        var item = new RewardItem
        {
            Id = 2,
            RewardName = "Redeemed Item",
            CostInPoints = 100,
            AvailableStock = 0,
            IsActive = true
        };
        context.RewardItems.Add(item);

        context.RewardRedemptions.Add(new RewardRedemption
        {
            Id = 201,
            RewardItemId = 2,
            EmployeeId = 1,
            VoucherCode = "CODE123",
            RedeemedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new DeleteRewardCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new GenericRepository<RewardRedemption>(context),
            new UnitOfWork(context)
        );

        var result = await handler.Handle(new DeleteRewardCommand(2), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var saved = await context.RewardItems.FindAsync(2);
        Assert.NotNull(saved);
        Assert.False(saved.IsActive); // Soft-deactivated
    }

    [Fact]
    public async Task Handle_ZeroRedemptionsAndZeroVouchers_PhysicallyDeletesReward()
    {
        using var context = CreateDbContext();
        var item = new RewardItem
        {
            Id = 3,
            RewardName = "Empty Item",
            CostInPoints = 50,
            AvailableStock = 0,
            IsActive = true
        };
        context.RewardItems.Add(item);
        await context.SaveChangesAsync();

        var handler = new DeleteRewardCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new GenericRepository<RewardRedemption>(context),
            new UnitOfWork(context)
        );

        var result = await handler.Handle(new DeleteRewardCommand(3), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var saved = await context.RewardItems.FindAsync(3);
        Assert.Null(saved); // Hard-deleted
    }

    [Fact]
    public async Task Controller_DeleteReward_Returns204NoContentOnSuccess()
    {
        var fakeMediator = new FakeMediator(Result.Success());
        var controller = new RewardsController(fakeMediator);

        var actionResult = await controller.DeleteReward(1, CancellationToken.None);

        Assert.IsType<NoContentResult>(actionResult);
    }

    [Fact]
    public async Task Controller_DeleteReward_Returns404NotFoundWhenMissing()
    {
        var fakeMediator = new FakeMediator(Result.NotFound("Reward item not found."));
        var controller = new RewardsController(fakeMediator);

        var actionResult = await controller.DeleteReward(999, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task Controller_DeleteReward_Returns409ConflictWhenActiveVouchersExist()
    {
        var fakeMediator = new FakeMediator(Result.Conflict("Cannot delete reward with active vouchers."));
        var controller = new RewardsController(fakeMediator);

        var actionResult = await controller.DeleteReward(1, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
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
