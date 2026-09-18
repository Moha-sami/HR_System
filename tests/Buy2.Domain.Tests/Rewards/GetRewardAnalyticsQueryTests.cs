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
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Rewards;

public class GetRewardAnalyticsQueryTests
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

        var handler = new GetRewardAnalyticsQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardRedemption>(context)
        );

        var result = await handler.Handle(new GetRewardAnalyticsQuery(999, new RewardTransactionFilterQueryDto()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
        Assert.Contains("Reward item not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_DefaultFilter_AggregatesMonthlyTimelineAndTransactions()
    {
        using var context = CreateDbContext();

        var rewardItem = new RewardItem
        {
            Id = 1,
            RewardName = "Coffee Voucher",
            Description = "Free coffee",
            CostInPoints = 50,
            IsActive = true
        };
        context.RewardItems.Add(rewardItem);

        var department = new Department { Id = 1, Name = "Operations" };
        var jobRole = new JobRole { Id = 1, Title = "Coordinator", DepartmentId = 1, Department = department };
        var employee = new Employee { Id = 1, FirstName = "Alice", LastName = "Smith", Email = "alice@example.com", EmployeeCode = "EMP-001", JobRole = jobRole };
        context.Departments.Add(department);
        context.JobRoles.Add(jobRole);
        context.Employees.Add(employee);

        var now = DateTimeOffset.UtcNow;
        var pt1 = new PointsTransaction { Id = 1, EmployeeId = 1, Amount = -50 };
        var pt2 = new PointsTransaction { Id = 2, EmployeeId = 1, Amount = -100 };
        context.PointsTransactions.AddRange(pt1, pt2);

        var redemption1 = new RewardRedemption
        {
            Id = 1,
            RewardItemId = 1,
            EmployeeId = 1,
            Employee = employee,
            PointTransactionId = 1,
            PointsTransaction = pt1,
            VoucherCode = "VOUCH-1",
            RedeemedAt = now.AddDays(-10)
        };

        var redemption2 = new RewardRedemption
        {
            Id = 2,
            RewardItemId = 1,
            EmployeeId = 1,
            Employee = employee,
            PointTransactionId = 2,
            PointsTransaction = pt2,
            VoucherCode = "VOUCH-2",
            RedeemedAt = now.AddDays(-2)
        };

        context.RewardRedemptions.AddRange(redemption1, redemption2);
        await context.SaveChangesAsync();

        var handler = new GetRewardAnalyticsQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardRedemption>(context)
        );

        var result = await handler.Handle(new GetRewardAnalyticsQuery(1, new RewardTransactionFilterQueryDto()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var analytics = result.Value;
        Assert.Equal(2, analytics.TotalCount);
        Assert.Equal(1, analytics.PageNumber);
        Assert.Equal(10, analytics.PageSize);
        Assert.Equal(1, analytics.TotalPages);

        Assert.NotEmpty(analytics.Timeline);
        Assert.Equal(2, analytics.Transactions.Count);

        var firstTx = analytics.Transactions.First();
        Assert.Equal("Alice Smith", firstTx.EmployeeName);
        Assert.Equal("EMP-001", firstTx.EmployeeCode);
        Assert.Equal("Operations", firstTx.DepartmentName);
        Assert.True(firstTx.PointsDeducted > 0);
    }

    [Fact]
    public async Task Handle_WeeklyTimelinePeriod_GroupsByWeek()
    {
        using var context = CreateDbContext();

        var rewardItem = new RewardItem { Id = 2, RewardName = "Meal Card", CostInPoints = 100, IsActive = true };
        context.RewardItems.Add(rewardItem);

        var employee = new Employee { Id = 2, FirstName = "Bob", LastName = "Jones", Email = "bob@example.com", EmployeeCode = "EMP-002" };
        context.Employees.Add(employee);

        var dateFrom = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var dateTo = new DateTimeOffset(2026, 9, 30, 23, 59, 59, TimeSpan.Zero);

        var pt = new PointsTransaction { Id = 10, EmployeeId = 2, Amount = -100 };
        context.PointsTransactions.Add(pt);

        context.RewardRedemptions.AddRange(
            new RewardRedemption
            {
                Id = 10,
                RewardItemId = 2,
                EmployeeId = 2,
                Employee = employee,
                PointTransactionId = 10,
                PointsTransaction = pt,
                VoucherCode = "V-WEEK-1",
                RedeemedAt = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero)
            },
            new RewardRedemption
            {
                Id = 11,
                RewardItemId = 2,
                EmployeeId = 2,
                Employee = employee,
                PointTransactionId = 10,
                PointsTransaction = pt,
                VoucherCode = "V-WEEK-2",
                RedeemedAt = new DateTimeOffset(2026, 9, 8, 14, 0, 0, TimeSpan.Zero)
            }
        );
        await context.SaveChangesAsync();

        var handler = new GetRewardAnalyticsQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardRedemption>(context)
        );

        var filter = new RewardTransactionFilterQueryDto(
            DateFrom: dateFrom,
            DateTo: dateTo,
            TimelinePeriod: "Weekly"
        );

        var result = await handler.Handle(new GetRewardAnalyticsQuery(2, filter), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var timeline = result.Value!.Timeline;
        Assert.Single(timeline); // Both fall in same week of Sept 7-13
        Assert.Equal(2, timeline.First().RedemptionCount);
        Assert.Equal(200, timeline.First().TotalPointsSpent);
    }

    [Fact]
    public async Task Handle_SearchTermFilter_FiltersByEmployeeNameCodeOrVoucher()
    {
        using var context = CreateDbContext();

        var rewardItem = new RewardItem { Id = 3, RewardName = "Cinema Pass", CostInPoints = 80, IsActive = true };
        context.RewardItems.Add(rewardItem);

        var dept = new Department { Id = 20, Name = "Marketing" };
        var role = new JobRole { Id = 20, Title = "Specialist", DepartmentId = 20, Department = dept };
        var emp1 = new Employee { Id = 10, FirstName = "Charlie", LastName = "Brown", Email = "c@b.com", EmployeeCode = "EMP-010", JobRole = role };
        var emp2 = new Employee { Id = 11, FirstName = "Diana", LastName = "Prince", Email = "d@p.com", EmployeeCode = "EMP-011", JobRole = role };
        context.Departments.Add(dept);
        context.JobRoles.Add(role);
        context.Employees.AddRange(emp1, emp2);

        var now = DateTimeOffset.UtcNow;
        var pt = new PointsTransaction { Id = 20, EmployeeId = 10, Amount = -80 };
        context.PointsTransactions.Add(pt);

        context.RewardRedemptions.AddRange(
            new RewardRedemption
            {
                Id = 20,
                RewardItemId = 3,
                EmployeeId = 10,
                Employee = emp1,
                PointTransactionId = 20,
                PointsTransaction = pt,
                VoucherCode = "CINEMA-ALPHA",
                RedeemedAt = now.AddDays(-1)
            },
            new RewardRedemption
            {
                Id = 21,
                RewardItemId = 3,
                EmployeeId = 11,
                Employee = emp2,
                PointTransactionId = 20,
                PointsTransaction = pt,
                VoucherCode = "CINEMA-BETA",
                RedeemedAt = now.AddDays(-2)
            }
        );
        await context.SaveChangesAsync();

        var handler = new GetRewardAnalyticsQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardRedemption>(context)
        );

        // Filter by Employee Name
        var resultName = await handler.Handle(new GetRewardAnalyticsQuery(3, new RewardTransactionFilterQueryDto(SearchTerm: "Diana")), CancellationToken.None);
        Assert.True(resultName.IsSuccess);
        Assert.Single(resultName.Value!.Transactions);
        Assert.Equal("Diana Prince", resultName.Value.Transactions.First().EmployeeName);

        // Filter by Voucher Code
        var resultVoucher = await handler.Handle(new GetRewardAnalyticsQuery(3, new RewardTransactionFilterQueryDto(SearchTerm: "ALPHA")), CancellationToken.None);
        Assert.True(resultVoucher.IsSuccess);
        Assert.Single(resultVoucher.Value!.Transactions);
        Assert.Equal("CINEMA-ALPHA", resultVoucher.Value.Transactions.First().VoucherCode);
    }

    [Fact]
    public async Task Handle_NullFilter_DefaultsSafelyWithoutException()
    {
        using var context = CreateDbContext();

        var rewardItem = new RewardItem { Id = 4, RewardName = "Test Reward", CostInPoints = 10, IsActive = true };
        context.RewardItems.Add(rewardItem);
        await context.SaveChangesAsync();

        var handler = new GetRewardAnalyticsQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardRedemption>(context)
        );

        var result = await handler.Handle(new GetRewardAnalyticsQuery(4, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Equal(0, result.Value.TotalPages);
    }

    [Fact]
    public async Task Handle_InvertedDateBounds_NormalizesBoundsCorrectly()
    {
        using var context = CreateDbContext();

        var rewardItem = new RewardItem { Id = 5, RewardName = "Gym Pass", CostInPoints = 120, IsActive = true };
        context.RewardItems.Add(rewardItem);

        var dept = new Department { Id = 30, Name = "HR" };
        var role = new JobRole { Id = 30, Title = "Generalist", DepartmentId = 30, Department = dept };
        var emp = new Employee { Id = 30, FirstName = "Eve", LastName = "Taylor", Email = "e@t.com", EmployeeCode = "EMP-030", JobRole = role };
        context.Departments.Add(dept);
        context.JobRoles.Add(role);
        context.Employees.Add(emp);

        var pt = new PointsTransaction { Id = 30, EmployeeId = 30, Amount = -120 };
        context.PointsTransactions.Add(pt);

        var redeemedAt = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);
        context.RewardRedemptions.Add(new RewardRedemption
        {
            Id = 30,
            RewardItemId = 5,
            EmployeeId = 30,
            Employee = emp,
            PointTransactionId = 30,
            PointsTransaction = pt,
            VoucherCode = "GYM-123",
            RedeemedAt = redeemedAt
        });
        await context.SaveChangesAsync();

        var handler = new GetRewardAnalyticsQueryHandler(
            new GenericRepository<RewardItem>(context),
            new GenericRepository<RewardRedemption>(context)
        );

        // DateFrom is later than DateTo (inverted)
        var invertedFilter = new RewardTransactionFilterQueryDto(
            DateFrom: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            DateTo: new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero)
        );

        var result = await handler.Handle(new GetRewardAnalyticsQuery(5, invertedFilter), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Transactions);
    }

    [Fact]
    public async Task Controller_GetRewardAnalytics_MapsNotFoundAndOkCorrectly()
    {
        var dummyDto = new RewardAnalyticsDto(
            new List<RedemptionTimelinePointDto>(),
            new List<RewardTransactionItemDto>(),
            0, 1, 10, 0
        );

        var okMediator = new FakeMediator(Result<RewardAnalyticsDto>.Success(dummyDto));
        var okController = new RewardsController(okMediator);

        var okResult = await okController.GetRewardAnalytics(1, new RewardTransactionFilterQueryDto(), CancellationToken.None);
        var okObj = Assert.IsType<OkObjectResult>(okResult);
        Assert.Equal(StatusCodes.Status200OK, okObj.StatusCode);
        Assert.Equal(dummyDto, okObj.Value);

        var notFoundMediator = new FakeMediator(Result<RewardAnalyticsDto>.NotFound("Reward item not found."));
        var notFoundController = new RewardsController(notFoundMediator);

        var notFoundResult = await notFoundController.GetRewardAnalytics(999, new RewardTransactionFilterQueryDto(), CancellationToken.None);
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
