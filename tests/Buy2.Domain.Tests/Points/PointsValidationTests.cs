using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Application.Validators.Points;
using FluentValidation.TestHelper;
using Xunit;

namespace Buy2.Domain.Tests.Points;

public class PointsValidationTests
{
    private readonly CreateManualPointsTransactionDtoValidator _manualValidator = new();
    private readonly PointsTransactionFilterQueryDtoValidator _filterValidator = new();
    private readonly SaveAutomationSettingsDtoValidator _saveSettingsValidator = new();

    #region CreateManualPointsTransactionDtoValidator Tests

    [Theory]
    [InlineData("Add", 50)]
    [InlineData("Deduct", 50)]
    [InlineData("Reward", 100)]
    [InlineData("Deduction", -50)]
    public void CreateManualTransaction_ValidPayload_ShouldNotHaveErrors(string type, decimal points)
    {
        var dto = new CreateManualPointsTransactionDto(1, type, points, "Performance award");
        var result = _manualValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateManualTransaction_InvalidEmployeeId_ShouldHaveError()
    {
        var dto = new CreateManualPointsTransactionDto(0, "Add", 50, "Comments here");
        var result = _manualValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.EmployeeId);
    }

    [Fact]
    public void CreateManualTransaction_EmptyComments_ShouldHaveError()
    {
        var dto = new CreateManualPointsTransactionDto(1, "Add", 50, "");
        var result = _manualValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Comments);
    }

    #endregion

    #region PointsTransactionFilterQueryDtoValidator Tests

    [Fact]
    public void FilterQuery_ValidPagingAndDates_ShouldNotHaveErrors()
    {
        var dto = new PointsTransactionFilterQueryDto(
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow,
            "thisMonth",
            "Ahmed",
            "Manual",
            "Add",
            "CreatedAt",
            "desc",
            1,
            20
        );
        var result = _filterValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void FilterQuery_InvalidDateRange_ShouldHaveError()
    {
        var dto = new PointsTransactionFilterQueryDto(
            DateTimeOffset.UtcNow.AddDays(10),
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null,
            null
        );
        var result = _filterValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void FilterQuery_InvalidPageSize_ShouldHaveError()
    {
        var dto = new PointsTransactionFilterQueryDto(
            null, null, null, null, null, null, null, null, 1, 500);
        var result = _filterValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    #endregion

    #region SaveAutomationSettingsDtoValidator Tests

    [Fact]
    public void SaveSettings_ValidRangesAndPriorities_ShouldNotHaveErrors()
    {
        var dto = new SaveAutomationSettingsDto(
            "Monthly",
            new List<AutomationSettingCategoryDto>
            {
                new AutomationSettingCategoryDto(
                    1,
                    "Performance",
                    "Score",
                    true,
                    new List<AutomationRangeDto>
                    {
                        new AutomationRangeDto(null, "Reward", 80m, 100m, null, 50),
                        new AutomationRangeDto(null, "Deduction", 0m, 50m, null, -20)
                    }
                ),
                new AutomationSettingCategoryDto(
                    2,
                    "Tasks",
                    "Deadlines",
                    true,
                    new List<AutomationRangeDto>
                    {
                        new AutomationRangeDto(null, "Deduction", null, null, "Urgent", -30),
                        new AutomationRangeDto(null, "Deduction", null, null, "High", -15)
                    }
                )
            }
        );

        var result = _saveSettingsValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SaveSettings_OverlappingRanges_ShouldHaveError()
    {
        var dto = new SaveAutomationSettingsDto(
            "Weekly",
            new List<AutomationSettingCategoryDto>
            {
                new AutomationSettingCategoryDto(
                    1,
                    "Performance",
                    "Score",
                    true,
                    new List<AutomationRangeDto>
                    {
                        new AutomationRangeDto(null, "Reward", 50m, 80m, null, 20),
                        new AutomationRangeDto(null, "Reward", 70m, 100m, null, 50)
                    }
                )
            }
        );

        var result = _saveSettingsValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor("Settings[0].Ranges");
    }

    [Fact]
    public void SaveSettings_DuplicatePriority_ShouldHaveError()
    {
        var dto = new SaveAutomationSettingsDto(
            "Daily",
            new List<AutomationSettingCategoryDto>
            {
                new AutomationSettingCategoryDto(
                    1,
                    "Tasks",
                    "Overdue",
                    true,
                    new List<AutomationRangeDto>
                    {
                        new AutomationRangeDto(null, "Deduction", null, null, "Urgent", -20),
                        new AutomationRangeDto(null, "Deduction", null, null, "Urgent", -30)
                    }
                )
            }
        );

        var result = _saveSettingsValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Settings);
    }

    #endregion
}
