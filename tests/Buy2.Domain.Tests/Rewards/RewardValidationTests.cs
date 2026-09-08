using System.IO;
using Buy2.Application.DTOs.Rewards.DTOs;
using Buy2.Application.Validators.Rewards;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Buy2.Domain.Tests.Rewards;

public class RewardValidationTests
{
    private readonly RewardCreateDtoValidator _createValidator = new();
    private readonly RewardUpdateDtoValidator _updateValidator = new();
    private readonly RewardFilterDtoValidator _filterValidator = new();
    private readonly BulkVoucherUploadDtoValidator _uploadValidator = new();

    private static IFormFile CreateFormFile(string fileName)
    {
        var content = new byte[] { 1, 2, 3 };
        return new FormFile(new MemoryStream(content), 0, content.Length, "file", fileName);
    }

    [Fact]
    public void CreateReward_ValidPayload_ShouldNotHaveErrors()
    {
        var dto = new RewardCreateDto(
            Name: "Amazon Gift Card",
            Description: "100 USD Gift Card",
            CategoryId: 1,
            BannerImageUrl: "https://example.com/banner.png",
            Points: 500,
            MonetaryValue: 100m,
            HowToRedeem: "Click link and redeem code.",
            TermsOfUse: "Valid for 1 year from issuance."
        );

        var result = _createValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", 1, 100, 10, "How", "Terms")]
    [InlineData("Valid Name", 0, 100, 10, "How", "Terms")]
    [InlineData("Valid Name", 1, 0, 10, "How", "Terms")]
    [InlineData("Valid Name", 1, -50, 10, "How", "Terms")]
    [InlineData("Valid Name", 1, 100, -5, "How", "Terms")]
    [InlineData("Valid Name", 1, 100, 10, "", "Terms")]
    [InlineData("Valid Name", 1, 100, 10, "How", "")]
    public void CreateReward_InvalidPayload_ShouldHaveErrors(
        string name, int categoryId, int points, decimal monetaryValue, string howToRedeem, string termsOfUse)
    {
        var dto = new RewardCreateDto(name, null, categoryId, null, points, monetaryValue, howToRedeem, termsOfUse);
        var result = _createValidator.TestValidate(dto);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void FilterReward_ValidBounds_ShouldPass()
    {
        var dto = new RewardFilterDto(
            Search: "gift",
            Status: "Active",
            FromDate: DateTimeOffset.UtcNow.AddDays(-10),
            ToDate: DateTimeOffset.UtcNow,
            Page: 1,
            PageSize: 50
        );

        var result = _filterValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void FilterReward_InvalidPaging_ShouldHaveErrors(int page, int pageSize)
    {
        var dto = new RewardFilterDto(null, null, null, null, page, pageSize);
        var result = _filterValidator.TestValidate(dto);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void FilterReward_ToDateEarlierThanFromDate_ShouldHaveError()
    {
        var dto = new RewardFilterDto(
            null, null,
            FromDate: DateTimeOffset.UtcNow,
            ToDate: DateTimeOffset.UtcNow.AddDays(-5),
            Page: 1,
            PageSize: 10
        );

        var result = _filterValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ToDate);
    }

    [Theory]
    [InlineData(".xlsx")]
    [InlineData(".xls")]
    [InlineData(".csv")]
    public void BulkVoucherUpload_AllowedExtension_ShouldPass(string extension)
    {
        var file = CreateFormFile($"vouchers{extension}");
        var dto = new RewardBulkExcelUpload(1, file);
        var result = _uploadValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".txt")]
    [InlineData(".exe")]
    [InlineData(".png")]
    public void BulkVoucherUpload_DisallowedExtension_ShouldHaveError(string extension)
    {
        var file = CreateFormFile($"vouchers{extension}");
        var dto = new RewardBulkExcelUpload(1, file);
        var result = _uploadValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.File);
    }

    [Fact]
    public void BulkVoucherUpload_NullFile_ShouldHaveError()
    {
        var dto = new RewardBulkExcelUpload(1, null!);
        var result = _uploadValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.File);
    }
}
