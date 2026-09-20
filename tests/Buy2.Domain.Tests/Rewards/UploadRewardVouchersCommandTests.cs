using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Api.Controllers;
using Buy2.Application.Common.Interfaces;
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

public class UploadRewardVouchersCommandTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static IFormFile CreateFakeFormFile(string fileName, long lengthBytes = 100)
    {
        var stream = new MemoryStream(new byte[lengthBytes]);
        return new FormFile(stream, 0, lengthBytes, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    private class FakeExcelVoucherParserService : IExcelVoucherParserService
    {
        private readonly List<string> _codes;
        private readonly Exception? _exceptionToThrow;

        public FakeExcelVoucherParserService(List<string> codes)
        {
            _codes = codes;
        }

        public FakeExcelVoucherParserService(Exception exceptionToThrow)
        {
            _codes = new List<string>();
            _exceptionToThrow = exceptionToThrow;
        }

        public Task<List<string>> UploadExcelFileAsync(IFormFile file, CancellationToken cancellation)
        {
            if (_exceptionToThrow != null)
            {
                throw _exceptionToThrow;
            }

            return Task.FromResult(_codes);
        }
    }

    [Fact]
    public async Task Handle_DtoNull_ReturnsFailure()
    {
        using var context = CreateDbContext();
        var handler = new UploadRewardVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new FakeExcelVoucherParserService(new List<string>()),
            new UnitOfWork(context)
        );

        var result = await handler.Handle(new UploadRewardVouchersCommand(1, null!), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Request data cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_RewardItemNotFound_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        var handler = new UploadRewardVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new FakeExcelVoucherParserService(new List<string>()),
            new UnitOfWork(context)
        );

        var dto = new UploadVouchersRequestDto(CreateFakeFormFile("vouchers.xlsx"), "100", null, false);
        var result = await handler.Handle(new UploadRewardVouchersCommand(999, dto), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
        Assert.Contains("Reward item not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_FileNullOrEmpty_ReturnsFailure()
    {
        using var context = CreateDbContext();
        var rewardItem = new RewardItem { Id = 1, RewardName = "Test Reward", CostInPoints = 50, IsActive = true };
        context.RewardItems.Add(rewardItem);
        await context.SaveChangesAsync();

        var handler = new UploadRewardVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new FakeExcelVoucherParserService(new List<string>()),
            new UnitOfWork(context)
        );

        var emptyFile = CreateFakeFormFile("vouchers.xlsx", lengthBytes: 0);
        var dto = new UploadVouchersRequestDto(emptyFile, "100", null, false);
        var result = await handler.Handle(new UploadRewardVouchersCommand(1, dto), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("File not found or empty", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_DisallowedExtension_ReturnsFailure()
    {
        using var context = CreateDbContext();
        var rewardItem = new RewardItem { Id = 1, RewardName = "Test Reward", CostInPoints = 50, IsActive = true };
        context.RewardItems.Add(rewardItem);
        await context.SaveChangesAsync();

        var handler = new UploadRewardVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new FakeExcelVoucherParserService(new List<string>()),
            new UnitOfWork(context)
        );

        var invalidFile = CreateFakeFormFile("vouchers.pdf", lengthBytes: 100);
        var dto = new UploadVouchersRequestDto(invalidFile, "100", null, false);
        var result = await handler.Handle(new UploadRewardVouchersCommand(1, dto), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid file format", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_ParserThrowsException_ReturnsFailure()
    {
        using var context = CreateDbContext();
        var rewardItem = new RewardItem { Id = 1, RewardName = "Test Reward", CostInPoints = 50, IsActive = true };
        context.RewardItems.Add(rewardItem);
        await context.SaveChangesAsync();

        var handler = new UploadRewardVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new FakeExcelVoucherParserService(new InvalidOperationException("Corrupt workbook")),
            new UnitOfWork(context)
        );

        var dto = new UploadVouchersRequestDto(CreateFakeFormFile("vouchers.xlsx"), "100", null, false);
        var result = await handler.Handle(new UploadRewardVouchersCommand(1, dto), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to parse voucher file: Corrupt workbook", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_NoCodesParsed_ReturnsFailure()
    {
        using var context = CreateDbContext();
        var rewardItem = new RewardItem { Id = 1, RewardName = "Test Reward", CostInPoints = 50, IsActive = true };
        context.RewardItems.Add(rewardItem);
        await context.SaveChangesAsync();

        var handler = new UploadRewardVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new FakeExcelVoucherParserService(new List<string>()),
            new UnitOfWork(context)
        );

        var dto = new UploadVouchersRequestDto(CreateFakeFormFile("vouchers.xlsx"), "100", null, false);
        var result = await handler.Handle(new UploadRewardVouchersCommand(1, dto), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("No voucher codes were found in the uploaded file", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_PreviewMode_ComputesDuplicatesAndReturnsPreviewWithoutSavingToDb()
    {
        using var context = CreateDbContext();
        var rewardItem = new RewardItem { Id = 1, RewardName = "Test Reward", CostInPoints = 50, AvailableStock = 5, IsActive = true };
        context.RewardItems.Add(rewardItem);
        context.RewardVouchers.Add(new RewardVoucher
        {
            Id = 1,
            RewardItemId = 1,
            BatchId = 50,
            Code = "VOUCH-EXISTING",
            Status = VoucherStatus.Available
        });
        await context.SaveChangesAsync();

        var parsedCodes = new List<string>
        {
            "VOUCH-NEW-1",
            "VOUCH-NEW-1", // in-file duplicate
            "VOUCH-EXISTING", // in-db duplicate
            "VOUCH-NEW-2"
        };

        var handler = new UploadRewardVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new FakeExcelVoucherParserService(parsedCodes),
            new UnitOfWork(context)
        );

        var dto = new UploadVouchersRequestDto(CreateFakeFormFile("vouchers.xlsx"), "101", null, Confirm: false);
        var result = await handler.Handle(new UploadRewardVouchersCommand(1, dto), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.Preview);
        Assert.Null(result.Value.Result);

        var preview = result.Value.Preview!;
        Assert.Equal(101, preview.BatchId);
        Assert.Equal(4, preview.TotalFound);
        Assert.Equal(1, preview.DuplicateInFileCount);
        Assert.Equal(1, preview.DuplicateInDbCount);
        Assert.Equal(2, preview.ValidCount);

        Assert.Equal(3, preview.Preview.Count);
        var existingPreviewItem = preview.Preview.First(p => p.VoucherCode == "VOUCH-EXISTING");
        Assert.False(existingPreviewItem.IsValid);
        Assert.Equal("Voucher already exists.", existingPreviewItem.Error);

        var newPreviewItem = preview.Preview.First(p => p.VoucherCode == "VOUCH-NEW-1");
        Assert.True(newPreviewItem.IsValid);
        Assert.Null(newPreviewItem.Error);

        // Verify DB unchanged
        Assert.Equal(1, await context.RewardVouchers.CountAsync());
        var reloadedItem = await context.RewardItems.FindAsync(1);
        Assert.Equal(5, reloadedItem!.AvailableStock);
    }

    [Fact]
    public async Task Handle_PreviewMode_InvalidBatchId_GeneratesFallbackBatchId()
    {
        using var context = CreateDbContext();
        var rewardItem = new RewardItem { Id = 1, RewardName = "Test Reward", CostInPoints = 50, IsActive = true };
        context.RewardItems.Add(rewardItem);
        await context.SaveChangesAsync();

        var handler = new UploadRewardVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new FakeExcelVoucherParserService(new List<string> { "CODE-1" }),
            new UnitOfWork(context)
        );

        var dto = new UploadVouchersRequestDto(CreateFakeFormFile("vouchers.csv"), "BATCH-STRING-123", null, Confirm: false);
        var result = await handler.Handle(new UploadRewardVouchersCommand(1, dto), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.Preview);
        Assert.True(result.Value.Preview!.BatchId >= 100000 && result.Value.Preview.BatchId <= 999999);
    }

    [Fact]
    public async Task Handle_CommitMode_ValidCodes_SavesVouchersAndUpdatesStock()
    {
        using var context = CreateDbContext();
        var rewardItem = new RewardItem { Id = 1, RewardName = "Test Reward", CostInPoints = 50, AvailableStock = 10, IsActive = true };
        context.RewardItems.Add(rewardItem);
        context.RewardVouchers.Add(new RewardVoucher
        {
            Id = 1,
            RewardItemId = 1,
            BatchId = 50,
            Code = "CODE-ALREADY-THERE",
            Status = VoucherStatus.Available
        });
        await context.SaveChangesAsync();

        var parsedCodes = new List<string>
        {
            "CODE-A",
            "CODE-B",
            "CODE-ALREADY-THERE"
        };

        var handler = new UploadRewardVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new FakeExcelVoucherParserService(parsedCodes),
            new UnitOfWork(context)
        );

        var expiry = DateTimeOffset.UtcNow.AddMonths(3);
        var dto = new UploadVouchersRequestDto(CreateFakeFormFile("vouchers.xlsx"), "202", expiry, Confirm: true);
        var result = await handler.Handle(new UploadRewardVouchersCommand(1, dto), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Null(result.Value.Preview);
        Assert.NotNull(result.Value.Result);

        var uploadResult = result.Value.Result!;
        Assert.Equal(202, uploadResult.BatchId);
        Assert.Equal(2, uploadResult.TotalUploaded);
        Assert.Equal(12, uploadResult.AvailableStock);
        Assert.Equal(expiry, uploadResult.ExpiryDate);

        // Verify database persistence
        Assert.Equal(3, await context.RewardVouchers.CountAsync());
        var addedVouchers = await context.RewardVouchers.Where(v => v.BatchId == 202).ToListAsync();
        Assert.Equal(2, addedVouchers.Count);
        Assert.All(addedVouchers, v =>
        {
            Assert.Equal(1, v.RewardItemId);
            Assert.Equal(VoucherStatus.Available, v.Status);
        });

        var updatedItem = await context.RewardItems.FindAsync(1);
        Assert.Equal(12, updatedItem!.AvailableStock);
    }

    [Fact]
    public async Task Handle_CommitMode_AllDuplicates_ReturnsFailure()
    {
        using var context = CreateDbContext();
        var rewardItem = new RewardItem { Id = 1, RewardName = "Test Reward", CostInPoints = 50, AvailableStock = 5, IsActive = true };
        context.RewardItems.Add(rewardItem);
        context.RewardVouchers.Add(new RewardVoucher
        {
            Id = 1,
            RewardItemId = 1,
            BatchId = 50,
            Code = "CODE-DUPLICATE",
            Status = VoucherStatus.Available
        });
        await context.SaveChangesAsync();

        var handler = new UploadRewardVouchersCommandHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardVoucher>(context),
            new FakeExcelVoucherParserService(new List<string> { "CODE-DUPLICATE" }),
            new UnitOfWork(context)
        );

        var dto = new UploadVouchersRequestDto(CreateFakeFormFile("vouchers.xlsx"), "303", null, Confirm: true);
        var result = await handler.Handle(new UploadRewardVouchersCommand(1, dto), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("No valid vouchers are available for upload", result.ErrorMessage);
    }

    [Fact]
    public async Task Controller_UploadVouchers_Returns404NotFound()
    {
        var fakeMediator = new FakeMediator(Result<UploadVouchersResponseDto>.NotFound("Reward item not found."));
        var controller = new RewardsController(fakeMediator);

        var dto = new UploadVouchersRequestDto(CreateFakeFormFile("vouchers.xlsx"), null, null, false);
        var actionResult = await controller.UploadVouchers(999, dto, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task Controller_UploadVouchers_Returns400BadRequest()
    {
        var fakeMediator = new FakeMediator(Result<UploadVouchersResponseDto>.ValidationFailure("File not found or empty."));
        var controller = new RewardsController(fakeMediator);

        var dto = new UploadVouchersRequestDto(CreateFakeFormFile("vouchers.xlsx"), null, null, false);
        var actionResult = await controller.UploadVouchers(1, dto, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Controller_UploadVouchers_Preview_Returns200Ok()
    {
        var previewDto = new UploadVouchersResponseDto(
            Preview: new VoucherUploadPreviewDto(123, 2, 2, 0, 0, new List<VoucherUploadPreviewItemDto>()),
            Result: null);

        var fakeMediator = new FakeMediator(Result<UploadVouchersResponseDto>.Success(previewDto));
        var controller = new RewardsController(fakeMediator);

        var dto = new UploadVouchersRequestDto(CreateFakeFormFile("vouchers.xlsx"), null, null, Confirm: false);
        var actionResult = await controller.UploadVouchers(1, dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(previewDto, okResult.Value);
    }

    [Fact]
    public async Task Controller_UploadVouchers_Confirm_Returns201Created()
    {
        var resultDto = new UploadVouchersResponseDto(
            Preview: null,
            Result: new VoucherUploadResultDto(123, 5, 15, null));

        var fakeMediator = new FakeMediator(Result<UploadVouchersResponseDto>.Success(resultDto));
        var controller = new RewardsController(fakeMediator);

        var dto = new UploadVouchersRequestDto(CreateFakeFormFile("vouchers.xlsx"), null, null, Confirm: true);
        var actionResult = await controller.UploadVouchers(1, dto, CancellationToken.None);

        var createdResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(resultDto, createdResult.Value);
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
