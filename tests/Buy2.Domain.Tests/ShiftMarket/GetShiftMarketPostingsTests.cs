using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Api.Controllers;
using Buy2.Application.DTOs.ShiftMarket;
using Buy2.Application.Features.ShiftMarket.GetShiftMarketPostings;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.ShiftMarket;

public class GetShiftMarketPostingsTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static GetShiftMarketPostingsQueryHandler CreateHandler(Buy2DbContext context)
    {
        return new GetShiftMarketPostingsQueryHandler(
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<ShiftClaim>(context),
            new GenericRepository<Site>(context),
            new GenericRepository<Employee>(context)
        );
    }

    [Fact]
    public async Task Handle_EmptyMarket_ReturnsEmptyPaginatedResponse()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var query = new GetShiftMarketPostingsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task Handle_PostingsWithNoClaims_ReturnsUnclaimedWithFullHeadcount()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var site = new Site { Id = 1, SiteName = "Downtown HQ" };
        var role = new JobRole { Id = 10, Title = "Front Desk Officer" };
        var shift = new ShiftEntity
        {
            Id = 101,
            SiteId = 1,
            JobRoleId = 10,
            JobRole = role,
            StartTime = new DateTimeOffset(2026, 9, 15, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 15, 16, 0, 0, TimeSpan.Zero),
            IsPublished = true,
            EmployeeId = null
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.ShiftEntities.Add(shift);
        await context.SaveChangesAsync();

        var query = new GetShiftMarketPostingsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        var posting = result.Items[0];
        Assert.Equal(101, posting.ShiftId);
        Assert.Equal("Downtown HQ", posting.SiteName);
        Assert.Equal("Front Desk Officer", posting.ShiftTitle);
        Assert.Equal("Front Desk Officer", posting.JobRoleTitle);
        Assert.Equal("Unclaimed", posting.Status);
        Assert.Equal(1, posting.RequiredHeadcount);
        Assert.Equal(1, posting.RemainingHeadcount);
        Assert.Equal(0, posting.TotalClaimRequestCount);
        Assert.Empty(posting.Claims);
        Assert.Equal("Operations Manager", posting.PostingCreator);
    }

    [Fact]
    public async Task Handle_PostingsWithPendingClaims_ReturnsPendingStatusWithNestedClaimDetails()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var site = new Site { Id = 2, SiteName = "Uptown Hub" };
        var role = new JobRole { Id = 20, Title = "Logistics Coordinator" };
        var employee = new Employee
        {
            Id = 55,
            FirstName = "Alice",
            LastName = "Smith",
            JobRoleId = 20,
            IsActive = true
        };

        var shift = new ShiftEntity
        {
            Id = 202,
            SiteId = 2,
            JobRoleId = 20,
            JobRole = role,
            StartTime = new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 16, 17, 0, 0, TimeSpan.Zero),
            IsPublished = true,
            EmployeeId = null
        };

        var submissionTime = new DateTime(2026, 9, 14, 12, 30, 0, DateTimeKind.Utc);
        var claim = new ShiftClaim
        {
            Id = 501,
            ShiftId = 202,
            EmployeeId = 55,
            Status = "Pending",
            OvertimeJustification = "Covering for peak load",
            CreatedAt = submissionTime
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(shift);
        context.ShiftClaims.Add(claim);
        await context.SaveChangesAsync();

        var query = new GetShiftMarketPostingsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        var posting = result.Items[0];
        Assert.Equal("Pending", posting.Status);
        Assert.Equal(1, posting.RemainingHeadcount);
        Assert.Equal(1, posting.TotalClaimRequestCount);
        Assert.Single(posting.Claims);

        var nestedClaim = posting.Claims[0];
        Assert.Equal(501, nestedClaim.ClaimId);
        Assert.Equal(55, nestedClaim.EmployeeId);
        Assert.Equal("Alice Smith", nestedClaim.EmployeeName);
        Assert.Equal("Pending", nestedClaim.ClaimStatus);
        Assert.Equal("Covering for peak load", nestedClaim.OvertimeJustification);
        Assert.Equal(new DateTimeOffset(submissionTime, TimeSpan.Zero), nestedClaim.ClaimSubmissionTimestamp);
    }

    [Fact]
    public async Task Handle_PostingsWithApprovedClaimOrAssignedEmployee_ReturnsCoveredWithZeroRemainingHeadcount()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var site = new Site { Id = 3, SiteName = "West Branch" };
        var role = new JobRole { Id = 30, Title = "Cashier" };

        // Shift 1: Covered by assigned EmployeeId
        var shift1 = new ShiftEntity
        {
            Id = 301,
            SiteId = 3,
            JobRoleId = 30,
            JobRole = role,
            StartTime = new DateTimeOffset(2026, 9, 17, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 17, 16, 0, 0, TimeSpan.Zero),
            IsPublished = true,
            EmployeeId = 88
        };

        // Shift 2: Covered by Approved claim
        var shift2 = new ShiftEntity
        {
            Id = 302,
            SiteId = 3,
            JobRoleId = 30,
            JobRole = role,
            StartTime = new DateTimeOffset(2026, 9, 18, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 18, 16, 0, 0, TimeSpan.Zero),
            IsPublished = true,
            EmployeeId = null
        };

        var approvedClaim = new ShiftClaim
        {
            Id = 701,
            ShiftId = 302,
            EmployeeId = 99,
            Status = "Approved",
            OvertimeJustification = "Approved by manager"
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.ShiftEntities.AddRange(shift1, shift2);
        context.ShiftClaims.Add(approvedClaim);
        await context.SaveChangesAsync();

        // Act
        var result = await handler.Handle(new GetShiftMarketPostingsQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, p =>
        {
            Assert.Equal("Covered", p.Status);
            Assert.Equal(0, p.RemainingHeadcount);
        });
    }

    [Fact]
    public async Task Handle_ProjectedOvertimeCalculation_CalculatesCorrectOvertimeHoursWhenExceedingThreshold()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var site = new Site { Id = 4, SiteName = "East Center" };
        var role = new JobRole { Id = 40, Title = "Technician" };

        var employee = new Employee
        {
            Id = 60,
            FirstName = "Bob",
            LastName = "Miller",
            JobRoleId = 40,
            IsActive = true,
            PayrollProfile = new PayrollProfile
            {
                EmployeeId = 60,
                SalaryType = "Hourly",
                PaymentAmount = 25m,
                OvertimeThresholdHours = 40m
            }
        };

        // Monday 2026-09-07 to Friday 2026-09-11: 4 shifts of 10 hours = 40 hours
        var shiftMon = new ShiftEntity
        {
            Id = 401,
            SiteId = 4,
            JobRoleId = 40,
            EmployeeId = 60,
            StartTime = new DateTimeOffset(2026, 9, 7, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 7, 18, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };
        var shiftTue = new ShiftEntity
        {
            Id = 402,
            SiteId = 4,
            JobRoleId = 40,
            EmployeeId = 60,
            StartTime = new DateTimeOffset(2026, 9, 8, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 8, 18, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };
        var shiftThu = new ShiftEntity
        {
            Id = 403,
            SiteId = 4,
            JobRoleId = 40,
            EmployeeId = 60,
            StartTime = new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 10, 18, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };
        var shiftFri = new ShiftEntity
        {
            Id = 404,
            SiteId = 4,
            JobRoleId = 40,
            EmployeeId = 60,
            StartTime = new DateTimeOffset(2026, 9, 11, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 11, 18, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };

        // Target shift in the SAME work week (Wednesday 2026-09-09): 8 hours
        var targetShift = new ShiftEntity
        {
            Id = 405,
            SiteId = 4,
            JobRoleId = 40,
            JobRole = role,
            StartTime = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 9, 17, 0, 0, TimeSpan.Zero),
            IsPublished = true,
            EmployeeId = null
        };

        var claim = new ShiftClaim
        {
            Id = 801,
            ShiftId = 405,
            EmployeeId = 60,
            Status = "Pending",
            CreatedAt = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc)
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.AddRange(shiftMon, shiftTue, shiftThu, shiftFri, targetShift);
        context.ShiftClaims.Add(claim);
        await context.SaveChangesAsync();

        // Act
        var result = await handler.Handle(new GetShiftMarketPostingsQuery(), CancellationToken.None);

        // Assert
        var posting = result.Items.First(p => p.ShiftId == 405);
        Assert.Single(posting.Claims);
        var claimDto = posting.Claims[0];
        // 40h existing + 8h this shift = 48h total weekly. Overtime threshold = 40h -> 8h overtime
        Assert.Equal(8.00m, claimDto.ProjectedOvertimeHours);
    }

    [Fact]
    public async Task Handle_DailyOvertimeCalculation_ReportsDailyExcessOver8Hours()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var site = new Site { Id = 5, SiteName = "South Facility" };
        var role = new JobRole { Id = 50, Title = "Security Specialist" };

        var employee = new Employee
        {
            Id = 70,
            FirstName = "Charlie",
            LastName = "Green",
            JobRoleId = 50,
            IsActive = true
        };

        // Shift is 10 hours on Saturday (no other shifts in the week)
        var targetShift = new ShiftEntity
        {
            Id = 501,
            SiteId = 5,
            JobRoleId = 50,
            JobRole = role,
            StartTime = new DateTimeOffset(2026, 9, 12, 7, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 12, 17, 0, 0, TimeSpan.Zero),
            IsPublished = true,
            EmployeeId = null
        };

        var claim = new ShiftClaim
        {
            Id = 901,
            ShiftId = 501,
            EmployeeId = 70,
            Status = "Pending"
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.Employees.Add(employee);
        context.ShiftEntities.Add(targetShift);
        context.ShiftClaims.Add(claim);
        await context.SaveChangesAsync();

        // Act
        var result = await handler.Handle(new GetShiftMarketPostingsQuery(), CancellationToken.None);

        // Assert
        var posting = Assert.Single(result.Items);
        var claimDto = Assert.Single(posting.Claims);
        // 10h duration > 8h -> 2h daily overtime
        Assert.Equal(2.00m, claimDto.ProjectedOvertimeHours);
    }

    [Fact]
    public async Task Handle_StatusFiltering_ReturnsOnlyMatchingPostings()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var site = new Site { Id = 6, SiteName = "Regional Depot" };
        var role = new JobRole { Id = 60, Title = "Handler" };

        // 1. Unclaimed
        var shiftUnclaimed = new ShiftEntity
        {
            Id = 601,
            SiteId = 6,
            JobRoleId = 60,
            JobRole = role,
            StartTime = new DateTimeOffset(2026, 9, 20, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 20, 16, 0, 0, TimeSpan.Zero),
            IsPublished = true,
            EmployeeId = null
        };

        // 2. Pending
        var shiftPending = new ShiftEntity
        {
            Id = 602,
            SiteId = 6,
            JobRoleId = 60,
            JobRole = role,
            StartTime = new DateTimeOffset(2026, 9, 21, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 21, 16, 0, 0, TimeSpan.Zero),
            IsPublished = true,
            EmployeeId = null
        };
        var pendingClaim = new ShiftClaim { Id = 910, ShiftId = 602, EmployeeId = 1, Status = "Pending" };

        // 3. Covered
        var shiftCovered = new ShiftEntity
        {
            Id = 603,
            SiteId = 6,
            JobRoleId = 60,
            JobRole = role,
            StartTime = new DateTimeOffset(2026, 9, 22, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 22, 16, 0, 0, TimeSpan.Zero),
            IsPublished = true,
            EmployeeId = 12
        };

        context.Sites.Add(site);
        context.JobRoles.Add(role);
        context.ShiftEntities.AddRange(shiftUnclaimed, shiftPending, shiftCovered);
        context.ShiftClaims.Add(pendingClaim);
        await context.SaveChangesAsync();

        // Act & Assert: "Pending"
        var pendingResult = await handler.Handle(new GetShiftMarketPostingsQuery(Status: "Pending"), CancellationToken.None);
        Assert.Single(pendingResult.Items);
        Assert.Equal(602, pendingResult.Items[0].ShiftId);

        // Act & Assert: "Covered"
        var coveredResult = await handler.Handle(new GetShiftMarketPostingsQuery(Status: "Covered"), CancellationToken.None);
        Assert.Single(coveredResult.Items);
        Assert.Equal(603, coveredResult.Items[0].ShiftId);

        // Act & Assert: "Unclaimed"
        var unclaimedResult = await handler.Handle(new GetShiftMarketPostingsQuery(Status: "Unclaimed"), CancellationToken.None);
        Assert.Single(unclaimedResult.Items);
        Assert.Equal(601, unclaimedResult.Items[0].ShiftId);

        // Act & Assert: "All"
        var allResult = await handler.Handle(new GetShiftMarketPostingsQuery(Status: "All"), CancellationToken.None);
        Assert.Equal(3, allResult.Items.Count);
    }

    [Fact]
    public async Task Handle_Searching_MatchesSiteRoleTitleAndCreator()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var creator = new Employee { Id = 77, FirstName = "Sarah", LastName = "Connor", IsActive = true };
        var template = new ShiftTemplate
        {
            Id = 10,
            Name = "Morning Rush Template",
            LastUpdatedByEmployeeId = 77,
            LastUpdatedByEmployee = creator
        };

        var siteA = new Site { Id = 71, SiteName = "Metro Alpha" };
        var siteB = new Site { Id = 72, SiteName = "Coastal Beta" };
        var roleA = new JobRole { Id = 701, Title = "Lead Pharmacist" };
        var roleB = new JobRole { Id = 702, Title = "Junior Clerk" };

        var shift1 = new ShiftEntity
        {
            Id = 7001,
            SiteId = 71,
            JobRoleId = 701,
            JobRole = roleA,
            ShiftTemplateId = 10,
            ShiftTemplate = template,
            StartTime = new DateTimeOffset(2026, 9, 23, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 23, 16, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };

        var shift2 = new ShiftEntity
        {
            Id = 7002,
            SiteId = 72,
            JobRoleId = 702,
            JobRole = roleB,
            StartTime = new DateTimeOffset(2026, 9, 24, 8, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 24, 16, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };

        context.Employees.Add(creator);
        context.ShiftTemplates.Add(template);
        context.Sites.AddRange(siteA, siteB);
        context.JobRoles.AddRange(roleA, roleB);
        context.ShiftEntities.AddRange(shift1, shift2);
        await context.SaveChangesAsync();

        // Search by site name
        var searchSite = await handler.Handle(new GetShiftMarketPostingsQuery(Search: "coastal"), CancellationToken.None);
        Assert.Single(searchSite.Items);
        Assert.Equal(7002, searchSite.Items[0].ShiftId);

        // Search by role title
        var searchRole = await handler.Handle(new GetShiftMarketPostingsQuery(Search: "pharmacist"), CancellationToken.None);
        Assert.Single(searchRole.Items);
        Assert.Equal(7001, searchRole.Items[0].ShiftId);

        // Search by shift title (template name)
        var searchTemplate = await handler.Handle(new GetShiftMarketPostingsQuery(Search: "rush"), CancellationToken.None);
        Assert.Single(searchTemplate.Items);
        Assert.Equal(7001, searchTemplate.Items[0].ShiftId);

        // Search by creator name
        var searchCreator = await handler.Handle(new GetShiftMarketPostingsQuery(Search: "connor"), CancellationToken.None);
        Assert.Single(searchCreator.Items);
        Assert.Equal(7001, searchCreator.Items[0].ShiftId);
        Assert.Equal("Sarah Connor", searchCreator.Items[0].PostingCreator);
    }

    [Fact]
    public async Task Handle_Pagination_SlicesItemsAndCalculatesTotalPages()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var site = new Site { Id = 80, SiteName = "Distribution Center" };
        var role = new JobRole { Id = 800, Title = "Operator" };
        context.Sites.Add(site);
        context.JobRoles.Add(role);

        var baseTime = new DateTimeOffset(2026, 9, 25, 8, 0, 0, TimeSpan.Zero);
        for (var i = 1; i <= 15; i++)
        {
            context.ShiftEntities.Add(new ShiftEntity
            {
                Id = 8000 + i,
                SiteId = 80,
                JobRoleId = 800,
                JobRole = role,
                StartTime = baseTime.AddHours(i),
                EndTime = baseTime.AddHours(i + 4),
                IsPublished = true
            });
        }
        await context.SaveChangesAsync();

        // Page 1 with pageSize = 10
        var page1 = await handler.Handle(new GetShiftMarketPostingsQuery(Page: 1, PageSize: 10), CancellationToken.None);
        Assert.Equal(15, page1.TotalCount);
        Assert.Equal(1, page1.Page);
        Assert.Equal(10, page1.PageSize);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(10, page1.Items.Count);

        // Page 2 with pageSize = 10
        var page2 = await handler.Handle(new GetShiftMarketPostingsQuery(Page: 2, PageSize: 10), CancellationToken.None);
        Assert.Equal(15, page2.TotalCount);
        Assert.Equal(2, page2.Page);
        Assert.Equal(10, page2.PageSize);
        Assert.Equal(2, page2.TotalPages);
        Assert.Equal(5, page2.Items.Count);
    }

    [Fact]
    public async Task ControllerEndpoint_GetShiftMarketPostings_ReturnsOkWithPaginatedResult()
    {
        // Arrange
        var expectedResponse = new ShiftMarketPaginatedResponseDto(
            Items: new List<ShiftMarketPostingDto>(),
            TotalCount: 0,
            Page: 1,
            PageSize: 10,
            TotalPages: 0
        );

        var fakeSender = new FakeMediatorSender(request =>
        {
            if (request is GetShiftMarketPostingsQuery)
            {
                return Task.FromResult<object>(expectedResponse);
            }
            throw new InvalidOperationException("Unexpected request type");
        });

        var controller = new OpenShiftsController(fakeSender);

        // Act
        var actionResult = await controller.GetShiftMarketPostings(
            status: "Pending",
            search: "Store",
            page: 1,
            pageSize: 10,
            siteId: 1,
            cancellationToken: CancellationToken.None
        );

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(200, okResult.StatusCode);
        var dto = Assert.IsType<ShiftMarketPaginatedResponseDto>(okResult.Value);
        Assert.Equal(expectedResponse, dto);
    }

    private class FakeMediatorSender : ISender
    {
        private readonly Func<object, Task<object>> _handler;

        public FakeMediatorSender(Func<object, Task<object>> handler)
        {
            _handler = handler;
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var result = await _handler(request);
            return (TResponse)result;
        }

        public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            await _handler(request);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
