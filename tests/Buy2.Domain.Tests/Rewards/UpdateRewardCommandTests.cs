using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Api.Controllers;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Models;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Application.Features.Rewards.Commands;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Rewards;

public class UpdateRewardCommandTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private class FakeFileStorageService : IFileStorageService
    {
        public Task<string> UploadAsync(string fileName, IFormFile file)
        {
            return Task.FromResult($"storage/uploads/{fileName}");
        }
    }

    private static IFormFile CreateFakeFormFile(string fileName, long lengthBytes = 100)
    {
        var stream = new MemoryStream(new byte[lengthBytes]);
        return new FormFile(stream, 0, lengthBytes, "imageFile", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }

    [Fact]
    public async Task Handle_ValidRequestWithoutNewImage_PreservesExistingBanner()
    {
        using var context = CreateDbContext();
        var category = new RewardCategory { Id = 1, Name = "Vouchers" };
        context.RewardCategories.Add(category);

        var existingItem = new RewardItem
        {
            Id = 10,
            RewardName = "Old Starbucks",
            Description = "Old Desc",
            CategoryId = 1,
            BannerImageUrl = "storage/existing.png",
            CostInPoints = 50,
            MonetaryValue = 5.0m,
            HowToRedeem = "Barcode",
            TermsOfUse = "No expiry",
            IsActive = false
        };
        context.RewardItems.Add(existingItem);
        await context.SaveChangesAsync();

        var handler = new UpdateRewardCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardCategory>(context),
            new FakeFileStorageService(),
            new UnitOfWork(context)
        );

        var dto = new RewardUpdateDto(
            Name: "New Starbucks 10",
            Description: "New Desc",
            CategoryId: 1,
            BannerImageUrl: null,
            Points: 100,
            MonetaryValue: 10.0m,
            HowToRedeem: "Barcode V2",
            TermsOfUse: "Valid 30 days"
        );

        var result = await handler.Handle(new UpdateRewardCommand(10, dto, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("New Starbucks 10", result.Value.Name);
        Assert.Equal("storage/existing.png", result.Value.ImageUrl);
        Assert.False(result.Value.IsActive);

        var saved = await context.RewardItems.FindAsync(10);
        Assert.NotNull(saved);
        Assert.Equal("New Starbucks 10", saved.RewardName);
        Assert.Equal("storage/existing.png", saved.BannerImageUrl);
        Assert.False(saved.IsActive);
    }

    [Fact]
    public async Task Handle_ValidRequestWithNewImage_UploadsAndUpdatesBannerUrl()
    {
        using var context = CreateDbContext();
        var category = new RewardCategory { Id = 1, Name = "Gadgets" };
        context.RewardCategories.Add(category);

        var existingItem = new RewardItem
        {
            Id = 11,
            RewardName = "Old Gadget",
            CategoryId = 1,
            BannerImageUrl = "old.png",
            CostInPoints = 100,
            MonetaryValue = 20m,
            HowToRedeem = "Ship",
            TermsOfUse = "None",
            IsActive = true
        };
        context.RewardItems.Add(existingItem);
        await context.SaveChangesAsync();

        var handler = new UpdateRewardCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardCategory>(context),
            new FakeFileStorageService(),
            new UnitOfWork(context)
        );

        var file = CreateFakeFormFile("updated-gadget.png", 300);
        var dto = new RewardUpdateDto(
            Name: "Updated Gadget",
            Description: "Gadget desc",
            CategoryId: 1,
            BannerImageUrl: null,
            Points: 150,
            MonetaryValue: 25m,
            HowToRedeem: "Ship",
            TermsOfUse: "None"
        );

        var result = await handler.Handle(new UpdateRewardCommand(11, dto, file), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.ImageUrl);
        Assert.StartsWith("storage/uploads/", result.Value.ImageUrl);
        Assert.EndsWith(".png", result.Value.ImageUrl);
    }

    [Fact]
    public async Task Handle_RewardNotFound_ReturnsNotFoundResult()
    {
        using var context = CreateDbContext();
        var category = new RewardCategory { Id = 1, Name = "Vouchers" };
        context.RewardCategories.Add(category);
        await context.SaveChangesAsync();

        var handler = new UpdateRewardCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardCategory>(context),
            new FakeFileStorageService(),
            new UnitOfWork(context)
        );

        var dto = new RewardUpdateDto("Any", "Desc", 1, null, 100, 10m, "App", "Terms");
        var result = await handler.Handle(new UpdateRewardCommand(999, dto, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
        Assert.Contains("Reward item not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsNotFoundResult()
    {
        using var context = CreateDbContext();
        var category = new RewardCategory { Id = 1, Name = "Vouchers" };
        context.RewardCategories.Add(category);

        var existingItem = new RewardItem
        {
            Id = 15,
            RewardName = "Item",
            CategoryId = 1,
            CostInPoints = 50,
            MonetaryValue = 5m,
            IsActive = true
        };
        context.RewardItems.Add(existingItem);
        await context.SaveChangesAsync();

        var handler = new UpdateRewardCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardCategory>(context),
            new FakeFileStorageService(),
            new UnitOfWork(context)
        );

        var dto = new RewardUpdateDto("Item", "Desc", 888, null, 50, 5m, "App", "Terms");
        var result = await handler.Handle(new UpdateRewardCommand(15, dto, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
        Assert.Contains("Category not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_DuplicateNameInSameCategory_ReturnsConflictResult()
    {
        using var context = CreateDbContext();
        var category = new RewardCategory { Id = 1, Name = "Vouchers" };
        context.RewardCategories.Add(category);

        context.RewardItems.Add(new RewardItem
        {
            Id = 20,
            RewardName = "Target Name",
            CategoryId = 1,
            CostInPoints = 100,
            MonetaryValue = 10m,
            IsActive = true
        });

        context.RewardItems.Add(new RewardItem
        {
            Id = 21,
            RewardName = "Current Name",
            CategoryId = 1,
            CostInPoints = 50,
            MonetaryValue = 5m,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var handler = new UpdateRewardCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardCategory>(context),
            new FakeFileStorageService(),
            new UnitOfWork(context)
        );

        var dto = new RewardUpdateDto("Target Name", "Desc", 1, null, 50, 5m, "App", "Terms");
        var result = await handler.Handle(new UpdateRewardCommand(21, dto, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsConflict);
        Assert.Contains("already exists", result.ErrorMessage);
    }

    [Fact]
    public async Task Controller_UpdateReward_Returns200OkOnSuccess()
    {
        var expectedProfile = new RewardProfileListDto(
            Id: 5,
            Name: "Updated Name",
            Description: "Updated Desc",
            ImageUrl: null,
            Category: "Tech",
            Points: 200,
            MonetaryValue: 20.0m,
            HowToRedeem: "Email",
            TermsOfUse: "Terms",
            IsActive: true
        );

        var fakeMediator = new FakeMediator(Result<RewardProfileListDto>.Success(expectedProfile));
        var controller = new RewardsController(fakeMediator);

        var dto = new RewardUpdateDto("Updated Name", "Updated Desc", 1, null, 200, 20m, "Email", "Terms");
        var actionResult = await controller.UpdateReward(5, dto, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(expectedProfile, okResult.Value);
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
