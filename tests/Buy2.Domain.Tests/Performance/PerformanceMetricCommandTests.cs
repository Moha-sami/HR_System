using System;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.DTOs.Employees;
using Buy2.Application.Features.Employees.PerformanceMetrics.CreateMetric;
using Buy2.Application.Features.Employees.PerformanceMetrics.GetMetrics;
using Buy2.Application.Features.Employees.PerformanceMetrics.UpdateMetric;
using Buy2.Application.Features.Employees.PerformanceSubmissions.SubmitScore;
using Buy2.Application.Validators.Employees;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Performance;

public class PerformanceMetricCommandTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private readonly CreatePerformanceMetricDtoValidator _createMetricValidator = new();
    private readonly UpdatePerformanceMetricDtoValidator _updateMetricValidator = new();
    private readonly CreatePerformanceSubmissionDtoValidator _submissionValidator = new();

    #region Validator Tests

    [Fact]
    public void CreateMetric_ValidPayload_ShouldNotHaveErrors()
    {
        var dto = new CreatePerformanceMetricDto("Sales Target", "Monthly target", 100m, 3m);
        var result = _createMetricValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void CreateMetric_InvalidWeight_ShouldHaveError(decimal weight)
    {
        var dto = new CreatePerformanceMetricDto("Sales Target", "Monthly target", 100m, weight);
        var result = _createMetricValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Weight);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void CreateMetric_InvalidTarget_ShouldHaveError(decimal target)
    {
        var dto = new CreatePerformanceMetricDto("Sales Target", "Monthly target", target, 1m);
        var result = _createMetricValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Target);
    }

    [Fact]
    public void CreateMetric_EmptyName_ShouldHaveError()
    {
        var dto = new CreatePerformanceMetricDto("", "Monthly target", 100m, 1m);
        var result = _createMetricValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void UpdateMetric_ValidPayload_ShouldNotHaveErrors()
    {
        var dto = new UpdatePerformanceMetricDto("Attendance", "Presence", 90m, 2m);
        var result = _updateMetricValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SubmitScore_ValidPayload_ShouldNotHaveErrors()
    {
        var dto = new CreatePerformanceSubmissionDto(1, 85m, "Good work");
        var result = _submissionValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(101)]
    public void SubmitScore_OutOfRangeScore_ShouldHaveError(decimal score)
    {
        var dto = new CreatePerformanceSubmissionDto(1, score, "Notes");
        var result = _submissionValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Score);
    }

    [Fact]
    public void SubmitScore_ZeroMetricId_ShouldHaveError()
    {
        var dto = new CreatePerformanceSubmissionDto(0, 80m, "Notes");
        var result = _submissionValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.MetricId);
    }

    #endregion

    #region CreateMetric Handler Tests

    [Fact]
    public async Task CreateMetric_ValidRequest_CreatesMetricSuccessfully()
    {
        using var context = CreateDbContext();
        var handler = new CreatePerformanceMetricCommandHandler(
            new GenericRepository<PerformanceMetric>(context),
            new UnitOfWork(context));

        var result = await handler.Handle(
            new CreatePerformanceMetricCommand(new CreatePerformanceMetricDto("Sales Target", "Monthly", 100m, 3m)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Sales Target", result.Value.Name);
        Assert.Equal(3m, result.Value.Weight);
        Assert.Equal(100m, result.Value.Target);

        var saved = await context.PerformanceMetrics.FirstOrDefaultAsync(m => m.Id == result.Value.Id);
        Assert.NotNull(saved);
        Assert.Equal(3m, saved.Weight);
    }

    [Fact]
    public async Task CreateMetric_DuplicateName_ReturnsConflict()
    {
        using var context = CreateDbContext();
        context.PerformanceMetrics.Add(new PerformanceMetric { Name = "Sales Target", Weight = 1m, Target = 100m });
        await context.SaveChangesAsync();

        var handler = new CreatePerformanceMetricCommandHandler(
            new GenericRepository<PerformanceMetric>(context),
            new UnitOfWork(context));

        var result = await handler.Handle(
            new CreatePerformanceMetricCommand(new CreatePerformanceMetricDto("Sales Target", "Other", 90m, 2m)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsConflict);
    }

    [Fact]
    public async Task CreateMetric_InvalidWeight_ReturnsValidationFailure()
    {
        using var context = CreateDbContext();
        var handler = new CreatePerformanceMetricCommandHandler(
            new GenericRepository<PerformanceMetric>(context),
            new UnitOfWork(context));

        var result = await handler.Handle(
            new CreatePerformanceMetricCommand(new CreatePerformanceMetricDto("Sales", "Desc", 100m, 0m)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsValidationError);
    }

    #endregion

    #region UpdateMetric Handler Tests

    [Fact]
    public async Task UpdateMetric_ExistingMetric_UpdatesWeightAndTarget()
    {
        using var context = CreateDbContext();
        context.PerformanceMetrics.Add(new PerformanceMetric { Id = 1, Name = "Sales", Weight = 1m, Target = 80m });
        await context.SaveChangesAsync();

        var handler = new UpdatePerformanceMetricCommandHandler(
            new GenericRepository<PerformanceMetric>(context),
            new UnitOfWork(context));

        var result = await handler.Handle(
            new UpdatePerformanceMetricCommand(1, new UpdatePerformanceMetricDto("Sales", "Updated", 100m, 4m)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(4m, result.Value.Weight);
        Assert.Equal(100m, result.Value.Target);
    }

    [Fact]
    public async Task UpdateMetric_MissingMetric_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        var handler = new UpdatePerformanceMetricCommandHandler(
            new GenericRepository<PerformanceMetric>(context),
            new UnitOfWork(context));

        var result = await handler.Handle(
            new UpdatePerformanceMetricCommand(999, new UpdatePerformanceMetricDto("Sales", "Desc", 100m, 1m)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    #endregion

    #region GetMetrics Handler Tests

    [Fact]
    public async Task GetMetrics_ReturnsOrderedMetrics()
    {
        using var context = CreateDbContext();
        context.PerformanceMetrics.Add(new PerformanceMetric { Name = "Zebra", Weight = 1m, Target = 100m });
        context.PerformanceMetrics.Add(new PerformanceMetric { Name = "Alpha", Weight = 2m, Target = 90m });
        await context.SaveChangesAsync();

        var handler = new GetPerformanceMetricsQueryHandler(
            new GenericRepository<PerformanceMetric>(context));

        var result = await handler.Handle(new GetPerformanceMetricsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Zebra", result[1].Name);
    }

    #endregion

    #region SubmitScore Handler Tests

    private static async Task SeedEmployeeAndMetric(Buy2DbContext context)
    {
        context.Employees.Add(new Employee { Id = 1, FirstName = "Sara", LastName = "Ali" });
        context.PerformanceMetrics.Add(new PerformanceMetric { Id = 1, Name = "Sales", Weight = 3m, Target = 100m });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task SubmitScore_ValidRequest_CreatesSubmissionWithSyncedFields()
    {
        using var context = CreateDbContext();
        await SeedEmployeeAndMetric(context);

        var handler = new SubmitPerformanceScoreCommandHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<PerformanceMetric>(context),
            new GenericRepository<PerformanceSubmission>(context),
            new UnitOfWork(context));

        var result = await handler.Handle(
            new SubmitPerformanceScoreCommand(1, new CreatePerformanceSubmissionDto(1, 85m, "Good work")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(85m, result.Value.Score);

        var saved = await context.PerformanceSubmissions.FirstOrDefaultAsync(s => s.Id == result.Value.Id);
        Assert.NotNull(saved);
        Assert.Equal(85m, saved.Score);
        Assert.Equal(saved.Score, saved.AchievedPercent);
    }

    [Fact]
    public async Task SubmitScore_MissingEmployee_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        context.PerformanceMetrics.Add(new PerformanceMetric { Id = 1, Name = "Sales", Weight = 1m, Target = 100m });
        await context.SaveChangesAsync();

        var handler = new SubmitPerformanceScoreCommandHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<PerformanceMetric>(context),
            new GenericRepository<PerformanceSubmission>(context),
            new UnitOfWork(context));

        var result = await handler.Handle(
            new SubmitPerformanceScoreCommand(999, new CreatePerformanceSubmissionDto(1, 80m, "Notes")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task SubmitScore_MissingMetric_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        context.Employees.Add(new Employee { Id = 1, FirstName = "Sara", LastName = "Ali" });
        await context.SaveChangesAsync();

        var handler = new SubmitPerformanceScoreCommandHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<PerformanceMetric>(context),
            new GenericRepository<PerformanceSubmission>(context),
            new UnitOfWork(context));

        var result = await handler.Handle(
            new SubmitPerformanceScoreCommand(1, new CreatePerformanceSubmissionDto(999, 80m, "Notes")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task SubmitScore_DeletedEmployee_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        context.Employees.Add(new Employee { Id = 1, FirstName = "Sara", LastName = "Ali", IsDeleted = true });
        context.PerformanceMetrics.Add(new PerformanceMetric { Id = 1, Name = "Sales", Weight = 1m, Target = 100m });
        await context.SaveChangesAsync();

        var handler = new SubmitPerformanceScoreCommandHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<PerformanceMetric>(context),
            new GenericRepository<PerformanceSubmission>(context),
            new UnitOfWork(context));

        var result = await handler.Handle(
            new SubmitPerformanceScoreCommand(1, new CreatePerformanceSubmissionDto(1, 80m, "Notes")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task SubmitScore_InvalidScore_ReturnsValidationFailure()
    {
        using var context = CreateDbContext();
        await SeedEmployeeAndMetric(context);

        var handler = new SubmitPerformanceScoreCommandHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<PerformanceMetric>(context),
            new GenericRepository<PerformanceSubmission>(context),
            new UnitOfWork(context));

        var result = await handler.Handle(
            new SubmitPerformanceScoreCommand(1, new CreatePerformanceSubmissionDto(1, 150m, "Notes")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsValidationError);
    }

    #endregion
}
