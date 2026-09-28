using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.Recognitions.Common;
using Buy2.Application.Features.Recognitions.Queries.GetRecognitions;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Buy2.Domain.Tests.Recognitions;

public class GetRecognitionsQueryTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetRecognitions_ReturnsPaginatedListWithRecipientAuthorAndPoints()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Sarah", LastName = "Connor", Email = "sarah@rebel.org" };
            var recipient = new Employee { FirstName = "John", LastName = "Connor", Email = "john@rebel.org" };
            context.Employees.AddRange(author, recipient);
            await context.SaveChangesAsync();

            var recognition = new Recognition
            {
                AuthorId = author.Id,
                RecipientId = recipient.Id,
                Title = "Mission Success",
                Narrative = "Completed defense mission ahead of schedule.",
                Badge = RecognitionLifecycleManager.BadgeExcellence,
                AwardedPoints = 500,
                Status = RecognitionLifecycleManager.StatusPublished,
                PublishedAt = DateTime.UtcNow
            };
            context.Recognitions.Add(recognition);
            await context.SaveChangesAsync();

            var repo = new GenericRepository<Recognition>(context);
            var handler = new GetRecognitionsHandler(repo);

            var query = new GetRecognitionsQuery(IsElevatedUser: true);
            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);

            var item = result.Items.First();
            Assert.Equal("Mission Success", item.Title);
            Assert.Equal("Sarah Connor", item.AuthorName);
            Assert.Equal("John Connor", item.RecipientName);
            Assert.Equal(500, item.AwardedPoints);
            Assert.Equal(RecognitionLifecycleManager.BadgeExcellence, item.Badge);
            Assert.Equal("Published", item.Status);
        }
    }

    [Fact]
    public async Task GetRecognitions_KeywordSearch_MatchesTitleOrDescriptionOrEmployeeName()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
            var rec1 = new Employee { FirstName = "Bob", LastName = "Johnson", Email = "bob@example.com" };
            var rec2 = new Employee { FirstName = "Charlie", LastName = "Brown", Email = "charlie@example.com" };
            context.Employees.AddRange(author, rec1, rec2);
            await context.SaveChangesAsync();

            var r1 = new Recognition
            {
                AuthorId = author.Id,
                RecipientId = rec1.Id,
                Title = "Outstanding Leadership",
                Narrative = "Led project X to delivery.",
                Status = RecognitionLifecycleManager.StatusPublished
            };
            var r2 = new Recognition
            {
                AuthorId = author.Id,
                RecipientId = rec2.Id,
                Title = "Bug Crusher",
                Narrative = "Resolved 50 customer issues.",
                Status = RecognitionLifecycleManager.StatusPublished
            };
            context.Recognitions.AddRange(r1, r2);
            await context.SaveChangesAsync();

            var repo = new GenericRepository<Recognition>(context);
            var handler = new GetRecognitionsHandler(repo);

            // Search by recipient name
            var searchRecipient = await handler.Handle(new GetRecognitionsQuery(Search: "Johnson", IsElevatedUser: true), CancellationToken.None);
            Assert.Equal(1, searchRecipient.TotalCount);
            Assert.Equal("Outstanding Leadership", searchRecipient.Items.First().Title);

            // Search by narrative keyword
            var searchNarrative = await handler.Handle(new GetRecognitionsQuery(Search: "customer", IsElevatedUser: true), CancellationToken.None);
            Assert.Equal(1, searchNarrative.TotalCount);
            Assert.Equal("Bug Crusher", searchNarrative.Items.First().Title);
        }
    }

    [Fact]
    public async Task GetRecognitions_StatusFilter_FiltersCorrectly()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Admin", LastName = "User", Email = "admin@corp.com" };
            var recipient = new Employee { FirstName = "Dev", LastName = "User", Email = "dev@corp.com" };
            context.Employees.AddRange(author, recipient);
            await context.SaveChangesAsync();

            var rPub = new Recognition { AuthorId = author.Id, RecipientId = recipient.Id, Title = "Pub", Narrative = "N1", Status = RecognitionLifecycleManager.StatusPublished };
            var rDraft = new Recognition { AuthorId = author.Id, RecipientId = recipient.Id, Title = "Draft", Narrative = "N2", Status = RecognitionLifecycleManager.StatusDraft };
            var rArch = new Recognition { AuthorId = author.Id, RecipientId = recipient.Id, Title = "Arch", Narrative = "N3", Status = RecognitionLifecycleManager.StatusArchived };
            context.Recognitions.AddRange(rPub, rDraft, rArch);
            await context.SaveChangesAsync();

            var repo = new GenericRepository<Recognition>(context);
            var handler = new GetRecognitionsHandler(repo);

            var resultDraft = await handler.Handle(new GetRecognitionsQuery(Status: "Drafted", IsElevatedUser: true), CancellationToken.None);
            Assert.Equal(1, resultDraft.TotalCount);
            Assert.Equal("Draft", resultDraft.Items.First().Title);

            var resultArch = await handler.Handle(new GetRecognitionsQuery(Status: "Archived", IsElevatedUser: true), CancellationToken.None);
            Assert.Equal(1, resultArch.TotalCount);
            Assert.Equal("Arch", resultArch.Items.First().Title);
        }
    }

    [Fact]
    public async Task GetRecognitions_NonElevatedUser_OnlySeesPublishedOrReleasedScheduled()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Manager", LastName = "Lead", Email = "mgr@corp.com" };
            var recipient = new Employee { FirstName = "Dev", LastName = "Peer", Email = "peer@corp.com" };
            context.Employees.AddRange(author, recipient);
            await context.SaveChangesAsync();

            var rPub = new Recognition { AuthorId = author.Id, RecipientId = recipient.Id, Title = "Published", Narrative = "N1", Status = RecognitionLifecycleManager.StatusPublished };
            var rDraft = new Recognition { AuthorId = author.Id, RecipientId = recipient.Id, Title = "Draft", Narrative = "N2", Status = RecognitionLifecycleManager.StatusDraft };
            var rSchedFuture = new Recognition
            {
                AuthorId = author.Id,
                RecipientId = recipient.Id,
                Title = "Future Scheduled",
                Narrative = "N3",
                Status = RecognitionLifecycleManager.StatusScheduled,
                ScheduledFor = DateTime.UtcNow.AddDays(5)
            };
            var rSchedPast = new Recognition
            {
                AuthorId = author.Id,
                RecipientId = recipient.Id,
                Title = "Released Scheduled",
                Narrative = "N4",
                Status = RecognitionLifecycleManager.StatusScheduled,
                ScheduledFor = DateTime.UtcNow.AddMinutes(-5)
            };
            context.Recognitions.AddRange(rPub, rDraft, rSchedFuture, rSchedPast);
            await context.SaveChangesAsync();

            var repo = new GenericRepository<Recognition>(context);
            var handler = new GetRecognitionsHandler(repo);

            var result = await handler.Handle(new GetRecognitionsQuery(IsElevatedUser: false), CancellationToken.None);

            Assert.Equal(2, result.TotalCount);
            Assert.Contains(result.Items, x => x.Title == "Published");
            Assert.Contains(result.Items, x => x.Title == "Released Scheduled");
            Assert.DoesNotContain(result.Items, x => x.Title == "Draft");
            Assert.DoesNotContain(result.Items, x => x.Title == "Future Scheduled");
        }
    }

    [Fact]
    public async Task GetRecognitions_SortingByPoints_AscendingAndDescending()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "HR", LastName = "Lead", Email = "hr@corp.com" };
            var rec = new Employee { FirstName = "Worker", LastName = "One", Email = "w@corp.com" };
            context.Employees.AddRange(author, rec);
            await context.SaveChangesAsync();

            var r1 = new Recognition { AuthorId = author.Id, RecipientId = rec.Id, Title = "Low", Narrative = "N", AwardedPoints = 100, Status = "Published" };
            var r2 = new Recognition { AuthorId = author.Id, RecipientId = rec.Id, Title = "High", Narrative = "N", AwardedPoints = 1000, Status = "Published" };
            var r3 = new Recognition { AuthorId = author.Id, RecipientId = rec.Id, Title = "Mid", Narrative = "N", AwardedPoints = 500, Status = "Published" };
            context.Recognitions.AddRange(r1, r2, r3);
            await context.SaveChangesAsync();

            var repo = new GenericRepository<Recognition>(context);
            var handler = new GetRecognitionsHandler(repo);

            // Descending
            var resultDesc = await handler.Handle(new GetRecognitionsQuery(SortBy: "points", SortDirection: "desc", IsElevatedUser: true), CancellationToken.None);
            Assert.Equal("High", resultDesc.Items.First().Title);
            Assert.Equal("Low", resultDesc.Items.Last().Title);

            // Ascending
            var resultAsc = await handler.Handle(new GetRecognitionsQuery(SortBy: "points", SortDirection: "asc", IsElevatedUser: true), CancellationToken.None);
            Assert.Equal("Low", resultAsc.Items.First().Title);
            Assert.Equal("High", resultAsc.Items.Last().Title);
        }
    }

    [Fact]
    public async Task GetRecognitions_ControllerEndpoint_Returns200OkWithPagedResult()
    {
        var items = new List<RecognitionSummaryDto>
        {
            new RecognitionSummaryDto(
                Id: 1,
                Title: "Top Innovator",
                Narrative: "Built an awesome AI feature.",
                Badge: "Innovator",
                Status: "Published",
                AuthorId: 2,
                AuthorName: "Sarah Connor",
                RecipientId: 3,
                RecipientName: "John Connor",
                RecipientAvatar: null,
                AwardedPoints: 300,
                ScheduledFor: null,
                PublishedAt: DateTime.UtcNow,
                LikesCount: 5,
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: null
            )
        };

        var pagedResult = new PaginatedListResult<RecognitionSummaryDto>(items, 1, 1, 10, 1);
        var mediator = new FakeRecognitionsMediator(pagedResult);
        var controller = new RecognitionsController(mediator);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "HR")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var actionResult = await controller.GetRecognitions(pageNumber: 1, pageSize: 10, cancellationToken: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedData = Assert.IsType<PaginatedListResult<RecognitionSummaryDto>>(okResult.Value);
        Assert.Equal(1, returnedData.TotalCount);
        Assert.Equal("Top Innovator", returnedData.Items.First().Title);
    }

    private class FakeRecognitionsMediator : ISender
    {
        private readonly object? _result;
        private readonly Exception? _exceptionToThrow;

        public FakeRecognitionsMediator(object? result = null, Exception? exceptionToThrow = null)
        {
            _result = result;
            _exceptionToThrow = exceptionToThrow;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (_exceptionToThrow != null)
                throw _exceptionToThrow;

            return Task.FromResult((TResponse)_result!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            if (_exceptionToThrow != null)
                throw _exceptionToThrow;

            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            if (_exceptionToThrow != null)
                throw _exceptionToThrow;

            return Task.FromResult(_result);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
