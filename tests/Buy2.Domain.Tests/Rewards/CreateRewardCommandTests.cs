using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Api.Controllers;
using Buy2.Application.Common.Interfaces;
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

public class CreateRewardCommandTests
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
    public async Task Handle_ValidRequestWithoutImage_CreatesRewardSuccessfully()
    {
        using var context = CreateDbContext();
        var category = new RewardCategory { Id = 1, Name = "Vouchers" };
        context.RewardCategories.Add(category);
        await context.SaveChangesAsync();

        var handler = new CreateRewardCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardCategory>(context),
            new FakeFileStorageService(),
            new UnitOfWork(context)
        );

        var dto = new RewardCreateDto(
            Name: "Starbucks 10 USD",
            Description: "Coffee voucher",
            CategoryId: 1,
            BannerImageUrl: null,
            Points: 100,
            MonetaryValue: 10.0m,
            HowToRedeem: "Show barcode",
            TermsOfUse: "Valid 30 days"
        );

        var result = await handler.Handle(new CreateRewardCommand(dto, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Starbucks 10 USD", result.Value.Name);
        Assert.Equal("Vouchers", result.Value.Category);
        Assert.Equal(100, result.Value.Points);
        Assert.Equal(10.0m, result.Value.MonetaryValue);
        Assert.True(result.Value.IsActive);

        var saved = await context.RewardItems.FirstOrDefaultAsync(r => r.Id == result.Value.Id);
        Assert.NotNull(saved);
        Assert.Equal("Starbucks 10 USD", saved.RewardName);
        Assert.Equal(0, saved.AvailableStock);
    }

    [Fact]
    public async Task Handle_ValidRequestWithImage_UploadsAndSetsBannerUrl()
    {
        using var context = CreateDbContext();
        var category = new RewardCategory { Id = 1, Name = "Electronics" };
        context.RewardCategories.Add(category);
        await context.SaveChangesAsync();

        var handler = new CreateRewardCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardCategory>(context),
            new FakeFileStorageService(),
            new UnitOfWork(context)
        );

        var file = CreateFakeFormFile("test-banner.png", 500);
        var dto = new RewardCreateDto(
            Name: "Wireless Headphones",
            Description: "Noise cancelling",
            CategoryId: 1,
            BannerImageUrl: null,
            Points: 500,
            MonetaryValue: 50.0m,
            HowToRedeem: "Collect at reception",
            TermsOfUse: "1 year warranty"
        );

        var result = await handler.Handle(new CreateRewardCommand(dto, file), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.ImageUrl);
        Assert.StartsWith("storage/uploads/", result.Value.ImageUrl);
        Assert.EndsWith(".png", result.Value.ImageUrl);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsNotFoundResult()
    {
        using var context = CreateDbContext();

        var handler = new CreateRewardCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardCategory>(context),
            new FakeFileStorageService(),
            new UnitOfWork(context)
        );

        var dto = new RewardCreateDto(
            Name: "Mystery Prize",
            Description: null,
            CategoryId: 999,
            BannerImageUrl: null,
            Points: 200,
            MonetaryValue: 20.0m,
            HowToRedeem: "Contact HR",
            TermsOfUse: "Non-refundable"
        );

        var result = await handler.Handle(new CreateRewardCommand(dto, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
        Assert.Contains("Category not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_DuplicateRewardNameInSameCategory_ReturnsConflictResult()
    {
        using var context = CreateDbContext();
        var category = new RewardCategory { Id = 1, Name = "Gift Cards" };
        context.RewardCategories.Add(category);
        context.RewardItems.Add(new RewardItem
        {
            RewardName = "Netflix 1 Month",
            CategoryId = 1,
            CostInPoints = 300,
            MonetaryValue = 15.0m,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var handler = new CreateRewardCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardCategory>(context),
            new FakeFileStorageService(),
            new UnitOfWork(context)
        );

        var dto = new RewardCreateDto(
            Name: "Netflix 1 Month",
            Description: "Streaming subscription",
            CategoryId: 1,
            BannerImageUrl: null,
            Points: 300,
            MonetaryValue: 15.0m,
            HowToRedeem: "Digital code",
            TermsOfUse: "Valid in region"
        );

        var result = await handler.Handle(new CreateRewardCommand(dto, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsConflict);
        Assert.Contains("already exists", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_InvalidImageExtension_ReturnsValidationFailure()
    {
        using var context = CreateDbContext();
        var category = new RewardCategory { Id = 1, Name = "Vouchers" };
        context.RewardCategories.Add(category);
        await context.SaveChangesAsync();

        var handler = new CreateRewardCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardCategory>(context),
            new FakeFileStorageService(),
            new UnitOfWork(context)
        );

        var file = CreateFakeFormFile("malicious.exe", 200);
        var dto = new RewardCreateDto(
            Name: "Test Reward",
            Description: null,
            CategoryId: 1,
            BannerImageUrl: null,
            Points: 100,
            MonetaryValue: 10.0m,
            HowToRedeem: "Code",
            TermsOfUse: "Terms"
        );

        var result = await handler.Handle(new CreateRewardCommand(dto, file), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsValidationError);
        Assert.Contains("Image extension should be one of", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_ImageExceedingSizeLimit_ReturnsValidationFailure()
    {
        using var context = CreateDbContext();
        var category = new RewardCategory { Id = 1, Name = "Vouchers" };
        context.RewardCategories.Add(category);
        await context.SaveChangesAsync();

        var handler = new CreateRewardCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardCategory>(context),
            new FakeFileStorageService(),
            new UnitOfWork(context)
        );

        // 2 MB file (limit is 1 MB)
        var file = CreateFakeFormFile("large-image.png", 2 * 1024 * 1024);
        var dto = new RewardCreateDto(
            Name: "Test Reward",
            Description: null,
            CategoryId: 1,
            BannerImageUrl: null,
            Points: 100,
            MonetaryValue: 10.0m,
            HowToRedeem: "Code",
            TermsOfUse: "Terms"
        );

        var result = await handler.Handle(new CreateRewardCommand(dto, file), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsValidationError);
        Assert.Contains("must not exceed 1 MB", result.ErrorMessage);
    }

    [Fact]
    public async Task Controller_CreateReward_Returns201CreatedOnSuccess()
    {
        var expectedProfile = new RewardProfileListDto(
            Id: 10,
            Name: "Test Voucher",
            Description: "Desc",
            ImageUrl: null,
            Category: "General",
            Points: 100,
            MonetaryValue: 10.0m,
            HowToRedeem: "App",
            TermsOfUse: "Terms",
            IsActive: true
        );

        var fakeMediator = new FakeMediator(Buy2.Application.Common.Models.Result<RewardProfileListDto>.Success(expectedProfile));
        var controller = new RewardsController(fakeMediator);

        var dto = new RewardCreateDto(
            Name: "Test Voucher",
            Description: "Desc",
            CategoryId: 1,
            BannerImageUrl: null,
            Points: 100,
            MonetaryValue: 10.0m,
            HowToRedeem: "App",
            TermsOfUse: "Terms"
        );

        var actionResult = await controller.CreateReward(dto, null, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        Assert.Equal(expectedProfile, objectResult.Value);
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
