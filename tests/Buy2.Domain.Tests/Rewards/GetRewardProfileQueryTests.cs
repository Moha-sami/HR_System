using System;
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

public class GetRewardProfileQueryTests
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

        var handler = new GetRewardProfileQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardRedemption>(context),
            new GenericRepository<RewardVoucher>(context)
        );

        var result = await handler.Handle(new GetRewardProfileQuery(999), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
        Assert.Contains("Reward item not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_RewardExists_ReturnsProfileAndComputedKpiStats()
    {
        using var context = CreateDbContext();

        var category = new RewardCategory { Id = 1, Name = "Gift Cards" };
        context.RewardCategories.Add(category);

        var rewardItem = new RewardItem
        {
            Id = 1,
            RewardName = "Amazon 25 USD",
            Description = "Digital gift card",
            CategoryId = 1,
            CostInPoints = 250,
            MonetaryValue = 25.0m,
            HowToRedeem = "Claim code",
            TermsOfUse = "No expiry",
            IsActive = true
        };
        context.RewardItems.Add(rewardItem);

        // 3 vouchers: 2 available, 1 redeemed
        context.RewardVouchers.AddRange(
            new RewardVoucher { Id = 1, RewardItemId = 1, Code = "V1", Status = VoucherStatus.Available },
            new RewardVoucher { Id = 2, RewardItemId = 1, Code = "V2", Status = VoucherStatus.Available },
            new RewardVoucher { Id = 3, RewardItemId = 1, Code = "V3", Status = VoucherStatus.Redeemed }
        );

        // Department & Employee
        var department = new Department { Id = 1, Name = "Engineering" };
        var jobRole = new JobRole { Id = 1, Title = "Dev", DepartmentId = 1, Department = department };
        var employee = new Employee { Id = 1, FirstName = "John", LastName = "Doe", Email = "j@d.com", EmployeeCode = "E1", JobRole = jobRole };
        context.Departments.Add(department);
        context.JobRoles.Add(jobRole);
        context.Employees.Add(employee);

        // Points transaction & Redemption
        var pt1 = new PointsTransaction { Id = 1, EmployeeId = 1, Amount = -250 };
        var pt2 = new PointsTransaction { Id = 2, EmployeeId = 1, Amount = -250 };
        context.PointsTransactions.AddRange(pt1, pt2);

        context.RewardRedemptions.AddRange(
            new RewardRedemption { Id = 1, RewardItemId = 1, EmployeeId = 1, Employee = employee, PointTransactionId = 1, PointsTransaction = pt1, VoucherCode = "V3" },
            new RewardRedemption { Id = 2, RewardItemId = 1, EmployeeId = 1, Employee = employee, PointTransactionId = 2, PointsTransaction = pt2, VoucherCode = "V0" }
        );

        await context.SaveChangesAsync();

        var handler = new GetRewardProfileQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardRedemption>(context),
            new GenericRepository<RewardVoucher>(context)
        );

        var result = await handler.Handle(new GetRewardProfileQuery(1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var profile = result.Value.Profile;
        Assert.Equal("Amazon 25 USD", profile.Name);
        Assert.Equal("Gift Cards", profile.Category);
        Assert.Equal(250, profile.Points);

        var kpis = result.Value.KpiStats;
        Assert.Equal(2, kpis.RedemptionCount);
        Assert.Equal("2/3", kpis.AvailableStock);
        Assert.Equal(500m, kpis.TotalCost);
        Assert.Equal(2, kpis.TopRedeemed);
        Assert.Equal(250, kpis.PointsValue);
    }

    [Fact]
    public async Task Handle_ZeroRedemptionsAndZeroVouchers_ReturnsZeroKpis()
    {
        using var context = CreateDbContext();
        var category = new RewardCategory { Id = 2, Name = "Swag" };
        context.RewardCategories.Add(category);

        var rewardItem = new RewardItem
        {
            Id = 2,
            RewardName = "Company T-Shirt",
            CategoryId = 2,
            CostInPoints = 50,
            MonetaryValue = 10m,
            HowToRedeem = "Pickup",
            TermsOfUse = "One per employee",
            IsActive = true
        };
        context.RewardItems.Add(rewardItem);
        await context.SaveChangesAsync();

        var handler = new GetRewardProfileQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardRedemption>(context),
            new GenericRepository<RewardVoucher>(context)
        );

        var result = await handler.Handle(new GetRewardProfileQuery(2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(0, result.Value.KpiStats.RedemptionCount);
        Assert.Equal("0/0", result.Value.KpiStats.AvailableStock);
        Assert.Equal(0m, result.Value.KpiStats.TotalCost);
        Assert.Equal(0, result.Value.KpiStats.TopRedeemed);
    }

    [Fact]
    public async Task Controller_GetRewardById_Returns200OkOnSuccess()
    {
        var profile = new RewardProfileListDto(1, "Reward", "Desc", null, "Cat", 100, 10m, "App", "Terms", true);
        var kpis = new RewardKpiStatistics(5, "2/5", 500m, 3, 100);
        var expected = new RewardProfileResponseDto(profile, kpis);

        var fakeMediator = new FakeMediator(Result<RewardProfileResponseDto>.Success(expected));
        var controller = new RewardsController(fakeMediator);

        var actionResult = await controller.GetRewardById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(expected, okResult.Value);
    }

    [Fact]
    public async Task Controller_GetRewardById_Returns404NotFoundWhenMissing()
    {
        var fakeMediator = new FakeMediator(Result<RewardProfileResponseDto>.NotFound("Reward item not found."));
        var controller = new RewardsController(fakeMediator);

        var actionResult = await controller.GetRewardById(999, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
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
