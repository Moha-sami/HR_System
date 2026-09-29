using Buy2.Api.Controllers;
using Buy2.Application.Features.Recognitions.Common;
using Buy2.Application.Features.Recognitions.Queries.GetRecognitionDetail;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Buy2.Domain.Tests.Recognitions;

public class GetRecognitionDetailQueryTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetRecognitionDetail_NonExistent_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var repo = new GenericRepository<Recognition>(context);
        var handler = new GetRecognitionDetailHandler(repo);

        var query = new GetRecognitionDetailQuery(Id: 9999);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task GetRecognitionDetail_DeletedRecognition_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var recognition = new Recognition
        {
            AuthorId = 1,
            RecipientId = 2,
            Title = "Deleted post",
            Narrative = "Not visible",
            IsDeleted = true,
            Status = RecognitionLifecycleManager.StatusPublished
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<Recognition>(context);
        var handler = new GetRecognitionDetailHandler(repo);

        var query = new GetRecognitionDetailQuery(Id: recognition.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task GetRecognitionDetail_Published_ReturnsRecipientProfileAndAuditAndPoints()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var dept = new Department { Name = "Engineering" };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        var jobRole = new JobRole { Title = "Principal Engineer", DepartmentId = dept.Id, Department = dept };
        context.JobRoles.Add(jobRole);
        await context.SaveChangesAsync();

        var author = new Employee { FirstName = "Jane", LastName = "Doe", Email = "jane@corp.com", ProfilePhotoUrl = "https://cdn/jane.jpg" };
        var recipient = new Employee { FirstName = "Bob", LastName = "Smith", Email = "bob@corp.com", ProfilePhotoUrl = "https://cdn/bob.jpg", JobRoleId = jobRole.Id, JobRole = jobRole };
        context.Employees.AddRange(author, recipient);
        await context.SaveChangesAsync();

        var recognition = new Recognition
        {
            AuthorId = author.Id,
            Author = author,
            RecipientId = recipient.Id,
            Recipient = recipient,
            Title = "Exceptional Leadership",
            Narrative = "Guided the system release flawlessly.",
            Badge = RecognitionLifecycleManager.BadgeLeadership,
            AwardedPoints = 1200,
            Status = RecognitionLifecycleManager.StatusPublished,
            PublishedAt = DateTime.UtcNow.AddHours(-1)
        };
        recognition.UpdatedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<Recognition>(context);
        var jobRoleRepo = new GenericRepository<JobRole>(context);
        var handler = new GetRecognitionDetailHandler(repo, jobRoleRepo);

        var query = new GetRecognitionDetailQuery(Id: recognition.Id);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(recognition.Id, result.Id);
        Assert.Equal("Exceptional Leadership", result.Title);
        Assert.Equal("Guided the system release flawlessly.", result.Narrative);
        Assert.Equal(1200, result.AwardedPoints);
        Assert.Equal(RecognitionLifecycleManager.BadgeLeadership, result.Badge);
        Assert.Equal(RecognitionLifecycleManager.StatusPublished, result.Status);

        // Recipient assertions
        Assert.NotNull(result.Recipient);
        Assert.Equal(recipient.Id, result.Recipient.Id);
        Assert.Equal("Bob Smith", result.Recipient.FullName);
        Assert.Equal("https://cdn/bob.jpg", result.Recipient.Avatar);
        Assert.Equal("Principal Engineer", result.Recipient.JobTitle);
        Assert.Equal("Engineering", result.Recipient.Department);
        Assert.Equal($"/api/v1/employees/{recipient.Id}", result.Recipient.ProfileUrl);

        // Audit assertions
        Assert.NotNull(result.Audit);
        Assert.Equal("Jane Doe", result.Audit.CreatorName);
        Assert.Equal("Jane Doe", result.Audit.LastUpdaterName);
        Assert.True(result.Audit.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetRecognitionDetail_Draft_NonElevatedNonAuthor_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var recognition = new Recognition
        {
            AuthorId = 10,
            RecipientId = 20,
            Title = "Draft Post",
            Narrative = "Secret draft",
            Status = RecognitionLifecycleManager.StatusDraft
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<Recognition>(context);
        var handler = new GetRecognitionDetailHandler(repo);

        var query = new GetRecognitionDetailQuery(Id: recognition.Id, CurrentUserId: 99, IsElevatedUser: false);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task GetRecognitionDetail_Draft_Author_Allowed()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var author = new Employee { FirstName = "Alice", LastName = "Wonderland", Email = "alice@corp.com" };
        var recipient = new Employee { FirstName = "Mad", LastName = "Hatter", Email = "hatter@corp.com" };
        context.Employees.AddRange(author, recipient);
        await context.SaveChangesAsync();

        var recognition = new Recognition
        {
            AuthorId = author.Id,
            Author = author,
            RecipientId = recipient.Id,
            Recipient = recipient,
            Title = "Draft Idea",
            Narrative = "Tea party excellence",
            Status = RecognitionLifecycleManager.StatusDraft
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<Recognition>(context);
        var handler = new GetRecognitionDetailHandler(repo);

        var query = new GetRecognitionDetailQuery(Id: recognition.Id, CurrentUserId: author.Id, IsElevatedUser: false);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(RecognitionLifecycleManager.StatusDraft, result.Status);
    }

    [Fact]
    public async Task GetRecognitionDetail_Draft_ElevatedUser_Allowed()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var author = new Employee { FirstName = "Alice", LastName = "Wonderland", Email = "alice@corp.com" };
        var recipient = new Employee { FirstName = "Mad", LastName = "Hatter", Email = "hatter@corp.com" };
        context.Employees.AddRange(author, recipient);
        await context.SaveChangesAsync();

        var recognition = new Recognition
        {
            AuthorId = author.Id,
            Author = author,
            RecipientId = recipient.Id,
            Recipient = recipient,
            Title = "Draft Idea",
            Narrative = "Tea party excellence",
            Status = RecognitionLifecycleManager.StatusDraft
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<Recognition>(context);
        var handler = new GetRecognitionDetailHandler(repo);

        var query = new GetRecognitionDetailQuery(Id: recognition.Id, CurrentUserId: 500, IsElevatedUser: true);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(RecognitionLifecycleManager.StatusDraft, result.Status);
    }

    [Fact]
    public async Task GetRecognitionDetail_FutureScheduled_NonElevated_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var recognition = new Recognition
        {
            AuthorId = 1,
            RecipientId = 2,
            Title = "Future Post",
            Narrative = "Will be revealed tomorrow",
            Status = RecognitionLifecycleManager.StatusScheduled,
            ScheduledFor = DateTime.UtcNow.AddDays(1)
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<Recognition>(context);
        var handler = new GetRecognitionDetailHandler(repo);

        var query = new GetRecognitionDetailQuery(Id: recognition.Id, CurrentUserId: 33, IsElevatedUser: false);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task GetRecognitionDetail_CalculatesReactionsAndUserReaction()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var author = new Employee { FirstName = "Paul", LastName = "Atreides", Email = "paul@dune.org" };
        var recipient = new Employee { FirstName = "Chani", LastName = "Kynes", Email = "chani@dune.org" };
        context.Employees.AddRange(author, recipient);
        await context.SaveChangesAsync();

        var recognition = new Recognition
        {
            AuthorId = author.Id,
            Author = author,
            RecipientId = recipient.Id,
            Recipient = recipient,
            Title = "Desert Power",
            Narrative = "Mastered the worm ride",
            Status = RecognitionLifecycleManager.StatusPublished
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var react1 = new Reaction { RecognitionId = recognition.Id, TargetType = "Recognition", TargetId = recognition.Id, EmployeeId = 101, ReactionType = "Like" };
        var react2 = new Reaction { RecognitionId = recognition.Id, TargetType = "Recognition", TargetId = recognition.Id, EmployeeId = 102, ReactionType = "Like" };
        var react3 = new Reaction { RecognitionId = recognition.Id, TargetType = "Recognition", TargetId = recognition.Id, EmployeeId = 103, ReactionType = "Celebrate" };
        context.Reactions.AddRange(react1, react2, react3);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<Recognition>(context);
        var handler = new GetRecognitionDetailHandler(repo);

        var query = new GetRecognitionDetailQuery(Id: recognition.Id, CurrentUserId: 101);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.LikesCount);
        Assert.Equal(2, result.ReactionBreakdown["Like"]);
        Assert.Equal(1, result.ReactionBreakdown["Celebrate"]);
        Assert.Equal("Like", result.UserReaction);
    }

    [Fact]
    public async Task Controller_GetRecognitionById_ReturnsOkWithDetail()
    {
        var detailDto = new RecognitionDetailDto(
            Id: 42,
            Title: "Star Performer",
            Narrative: "Great work!",
            AwardedPoints: 300,
            Badge: "Innovation",
            Status: "Published",
            ScheduledFor: null,
            PublishedAt: DateTime.UtcNow,
            AttachmentUrl: null,
            Recipient: new RecognitionRecipientDetailDto(1, "Recipient Name", null, null, "Developer", "IT", "/api/v1/employees/1"),
            Audit: new RecognitionAuditDto("Author Name", DateTime.UtcNow, null, null),
            LikesCount: 5,
            ReactionBreakdown: new Dictionary<string, int> { ["Like"] = 5 },
            UserReaction: "Like",
            AuthorId: 2,
            AuthorName: "Author Name",
            AuthorAvatar: null,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null
        );

        var fakeMediator = new FakeRecognitionsMediator(result: detailDto);
        var controller = new RecognitionsController(fakeMediator);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99"),
            new Claim(ClaimTypes.Role, "Employee")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var actionResult = await controller.GetRecognitionById(42, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<RecognitionDetailDto>(okResult.Value);

        Assert.Equal(42, response.Id);
        Assert.Equal("Star Performer", response.Title);
        Assert.Equal(300, response.AwardedPoints);
    }

    [Fact]
    public async Task Controller_GetRecognitionById_NotFound_Returns404()
    {
        var fakeMediator = new FakeRecognitionsMediator(exceptionToThrow: new KeyNotFoundException("Recognition 999 not found."));
        var controller = new RecognitionsController(fakeMediator);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var actionResult = await controller.GetRecognitionById(999, CancellationToken.None);
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
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
