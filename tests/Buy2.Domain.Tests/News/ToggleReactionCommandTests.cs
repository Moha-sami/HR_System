using Buy2.Api.Controllers;
using Buy2.Application.DTOs.News;
using Buy2.Application.Features.News.Commands.ToggleReaction;
using Buy2.Application.Features.News.Common;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using FluentValidation;
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

namespace Buy2.Domain.Tests.News;

public class ToggleReactionCommandTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task ToggleReaction_NewReaction_AddsReactionAndUpdatesBreakdown()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var user = new Employee { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
            context.Employees.Add(user);

            var post = new Post
            {
                Title = "Published News",
                Content = "Content",
                PostType = "News",
                Status = NewsLifecycleManager.StatusPublished,
                LikesCount = 0
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var reactRepo = new GenericRepository<Reaction>(context);
            var postRepo = new GenericRepository<Post>(context);
            var commRepo = new GenericRepository<Comment>(context);
            var recogRepo = new GenericRepository<Recognition>(context);
            var empRepo = new GenericRepository<Employee>(context);

            var handler = new ToggleReactionHandler(reactRepo, postRepo, commRepo, recogRepo, empRepo, uow);

            var command = new ToggleReactionCommand(
                TargetType: "Post",
                TargetId: post.Id,
                ReactionType: "Like",
                CallerId: user.Id
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.IsActive);
            Assert.Equal("Like", result.UserReaction);
            Assert.Equal(1, result.TotalCount);
            Assert.True(result.ReactionBreakdown.ContainsKey("Like"));
            Assert.Equal(1, result.ReactionBreakdown["Like"]);

            var updatedPost = await context.Posts.FindAsync(post.Id);
            Assert.NotNull(updatedPost);
            Assert.Equal(1, updatedPost.LikesCount);
        }
    }

    [Fact]
    public async Task ToggleReaction_SameReaction_RemovesReactionToggleOff()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var user = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
            context.Employees.Add(user);

            var post = new Post { Title = "Post", Content = "Content", PostType = "News", Status = NewsLifecycleManager.StatusPublished };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var comment = new Comment { PostId = post.Id, AuthorId = user.Id, Content = "Great post!" };
            context.Comments.Add(comment);
            await context.SaveChangesAsync();

            var existingReaction = new Reaction
            {
                TargetType = "Comment",
                TargetId = comment.Id,
                CommentId = comment.Id,
                EmployeeId = user.Id,
                ReactionType = "Heart"
            };
            context.Reactions.Add(existingReaction);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var reactRepo = new GenericRepository<Reaction>(context);
            var postRepo = new GenericRepository<Post>(context);
            var commRepo = new GenericRepository<Comment>(context);
            var recogRepo = new GenericRepository<Recognition>(context);
            var empRepo = new GenericRepository<Employee>(context);

            var handler = new ToggleReactionHandler(reactRepo, postRepo, commRepo, recogRepo, empRepo, uow);

            var command = new ToggleReactionCommand(
                TargetType: "Comment",
                TargetId: comment.Id,
                ReactionType: "heart", // case-insensitive
                CallerId: user.Id
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.False(result.IsActive);
            Assert.Null(result.UserReaction);
            Assert.Equal(0, result.TotalCount);

            var reactionsInDb = await context.Reactions.CountAsync();
            Assert.Equal(0, reactionsInDb);
        }
    }

    [Fact]
    public async Task ToggleReaction_DifferentReaction_SwitchesReactionMutuallyExclusive()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var user = new Employee { FirstName = "Bob", LastName = "Builder", Email = "bob@example.com" };
            context.Employees.Add(user);

            var post = new Post
            {
                Title = "Company Update",
                Content = "Body",
                PostType = "News",
                Status = NewsLifecycleManager.StatusPublished,
                LikesCount = 1
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var existingReaction = new Reaction
            {
                TargetType = "Post",
                TargetId = post.Id,
                PostId = post.Id,
                EmployeeId = user.Id,
                ReactionType = "Like"
            };
            context.Reactions.Add(existingReaction);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var reactRepo = new GenericRepository<Reaction>(context);
            var postRepo = new GenericRepository<Post>(context);
            var commRepo = new GenericRepository<Comment>(context);
            var recogRepo = new GenericRepository<Recognition>(context);
            var empRepo = new GenericRepository<Employee>(context);

            var handler = new ToggleReactionHandler(reactRepo, postRepo, commRepo, recogRepo, empRepo, uow);

            var command = new ToggleReactionCommand(
                TargetType: "Post",
                TargetId: post.Id,
                ReactionType: "Wow",
                CallerId: user.Id
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.IsActive);
            Assert.Equal("Wow", result.UserReaction);
            Assert.Equal(1, result.TotalCount);
            Assert.Equal(1, result.ReactionBreakdown["Wow"]);
            Assert.False(result.ReactionBreakdown.ContainsKey("Like"));

            var updatedPost = await context.Posts.FindAsync(post.Id);
            Assert.NotNull(updatedPost);
            Assert.Equal(0, updatedPost.LikesCount); // Like was switched to Wow
        }
    }

    [Fact]
    public async Task ToggleReaction_RecognitionTarget_Success()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Sender", LastName = "User", Email = "sender@example.com" };
            var recipient = new Employee { FirstName = "Recipient", LastName = "User", Email = "recipient@example.com" };
            context.Employees.AddRange(author, recipient);
            await context.SaveChangesAsync();

            var recognition = new Recognition
            {
                AuthorId = author.Id,
                RecipientId = recipient.Id,
                Badge = "TeamPlayer",
                Title = "Kudos",
                Narrative = "Great job on the project!"
            };
            context.Recognitions.Add(recognition);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var reactRepo = new GenericRepository<Reaction>(context);
            var postRepo = new GenericRepository<Post>(context);
            var commRepo = new GenericRepository<Comment>(context);
            var recogRepo = new GenericRepository<Recognition>(context);
            var empRepo = new GenericRepository<Employee>(context);

            var handler = new ToggleReactionHandler(reactRepo, postRepo, commRepo, recogRepo, empRepo, uow);

            var command = new ToggleReactionCommand(
                TargetType: "Recognition",
                TargetId: recognition.Id,
                ReactionType: "Heart",
                CallerId: author.Id
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.IsActive);
            Assert.Equal("Heart", result.UserReaction);
            Assert.Equal(1, result.TotalCount);
        }
    }

    [Fact]
    public async Task ToggleReaction_NonExistentPost_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var user = new Employee { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        context.Employees.Add(user);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var reactRepo = new GenericRepository<Reaction>(context);
        var postRepo = new GenericRepository<Post>(context);
        var commRepo = new GenericRepository<Comment>(context);
        var recogRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var handler = new ToggleReactionHandler(reactRepo, postRepo, commRepo, recogRepo, empRepo, uow);

        var command = new ToggleReactionCommand(
            TargetType: "Post",
            TargetId: 9999,
            ReactionType: "Like",
            CallerId: user.Id
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task ToggleReaction_NonExistentComment_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var user = new Employee { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        context.Employees.Add(user);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var reactRepo = new GenericRepository<Reaction>(context);
        var postRepo = new GenericRepository<Post>(context);
        var commRepo = new GenericRepository<Comment>(context);
        var recogRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var handler = new ToggleReactionHandler(reactRepo, postRepo, commRepo, recogRepo, empRepo, uow);

        var command = new ToggleReactionCommand(
            TargetType: "Comment",
            TargetId: 9999,
            ReactionType: "Dislike",
            CallerId: user.Id
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task ToggleReaction_InvalidTargetType_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var reactRepo = new GenericRepository<Reaction>(context);
        var postRepo = new GenericRepository<Post>(context);
        var commRepo = new GenericRepository<Comment>(context);
        var recogRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var handler = new ToggleReactionHandler(reactRepo, postRepo, commRepo, recogRepo, empRepo, uow);

        var command = new ToggleReactionCommand(
            TargetType: "InvalidType",
            TargetId: 1,
            ReactionType: "Like"
        );

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Theory]
    [InlineData("thumbsup")]
    [InlineData("smile")]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ToggleReaction_InvalidReactionType_ThrowsValidationException(string invalidReaction)
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var reactRepo = new GenericRepository<Reaction>(context);
        var postRepo = new GenericRepository<Post>(context);
        var commRepo = new GenericRepository<Comment>(context);
        var recogRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var handler = new ToggleReactionHandler(reactRepo, postRepo, commRepo, recogRepo, empRepo, uow);

        var command = new ToggleReactionCommand(
            TargetType: "Post",
            TargetId: 1,
            ReactionType: invalidReaction
        );

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task ToggleReaction_ControllerEndpoint_Success_Returns200Ok()
    {
        var expectedSummary = new ReactionSummaryDto(
            TargetType: "Post",
            TargetId: 10,
            EmployeeId: 5,
            UserReaction: "Like",
            ActiveReaction: "Like",
            IsActive: true,
            TotalCount: 1,
            ReactionBreakdown: new Dictionary<string, int> { { "Like", 1 } },
            Message: "Reaction 'Like' added."
        );

        var mediator = new FakeReactionsMediator(expectedSummary);
        var controller = new ReactionsController(mediator);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "5")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var requestDto = new ToggleReactionRequestDto("Like");

        var actionResult = await controller.ToggleReaction("Post", 10, requestDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedDto = Assert.IsType<ReactionSummaryDto>(okResult.Value);
        Assert.Equal("Like", returnedDto.UserReaction);
        Assert.Equal(1, returnedDto.TotalCount);
    }

    [Fact]
    public async Task ToggleReaction_ControllerEndpoint_NotFound_Returns404NotFound()
    {
        var mediator = new FakeReactionsMediator(exceptionToThrow: new KeyNotFoundException("Post not found"));
        var controller = new ReactionsController(mediator);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "5")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var requestDto = new ToggleReactionRequestDto("Like");

        var actionResult = await controller.ToggleReaction("Post", 999, requestDto, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    private class FakeReactionsMediator : ISender
    {
        private readonly object? _result;
        private readonly Exception? _exceptionToThrow;

        public FakeReactionsMediator(object? result = null, Exception? exceptionToThrow = null)
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
