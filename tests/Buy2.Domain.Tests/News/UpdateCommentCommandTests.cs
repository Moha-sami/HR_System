using Buy2.Api.Controllers;
using Buy2.Application.DTOs.News;
using Buy2.Application.Features.News.Commands.UpdateComment;
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

public class UpdateCommentCommandTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task UpdateComment_ValidAuthor_UpdatesContentAndTimestamp()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
            context.Employees.Add(author);

            var post = new Post { Title = "Post", Content = "Content", PostType = "News", Status = NewsLifecycleManager.StatusPublished };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var originalCreatedAt = DateTime.UtcNow.AddHours(-2);
            var comment = new Comment
            {
                PostId = post.Id,
                AuthorId = author.Id,
                Content = "Original comment text",
                CreatedAt = originalCreatedAt,
                UpdatedAt = null,
                IsDeleted = false,
                IsModerated = false
            };
            context.Comments.Add(comment);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var commentRepo = new GenericRepository<Comment>(context);
            var validator = new UpdateCommentValidator();

            var handler = new UpdateCommentHandler(commentRepo, uow, validator);

            var command = new UpdateCommentCommand(
                CommentId: comment.Id,
                Content: "Revised and improved comment text",
                CallerId: author.Id
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(comment.Id, result.Id);
            Assert.Equal("Revised and improved comment text", result.Content);
            Assert.NotNull(result.UpdatedAt);
            Assert.True(result.UpdatedAt > originalCreatedAt);
            Assert.Equal("Alice Smith", result.AuthorName);
            var updated = await context.Comments.FindAsync(comment.Id);
            Assert.NotNull(updated);
            Assert.Equal("Revised and improved comment text", updated.Content);
            Assert.NotNull(updated.UpdatedAt);
        }
    }

    [Fact]
    public async Task UpdateComment_DifferentUser_ThrowsUnauthorizedAccessException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var author = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
        var intruder = new Employee { FirstName = "Bob", LastName = "Intruder", Email = "bob@example.com" };
        context.Employees.AddRange(author, intruder);

        var post = new Post { Title = "Post", Content = "Content", PostType = "News", Status = NewsLifecycleManager.StatusPublished };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var comment = new Comment
        {
            PostId = post.Id,
            AuthorId = author.Id,
            Content = "Original comment",
            IsDeleted = false,
            IsModerated = false
        };
        context.Comments.Add(comment);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var commentRepo = new GenericRepository<Comment>(context);

        var handler = new UpdateCommentHandler(commentRepo, uow);

        var command = new UpdateCommentCommand(
            CommentId: comment.Id,
            Content: "Hacked comment text",
            CallerId: intruder.Id
        );

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateComment_NullCallerId_ThrowsUnauthorizedAccessException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var author = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
        context.Employees.Add(author);

        var post = new Post { Title = "Post", Content = "Content", PostType = "News", Status = NewsLifecycleManager.StatusPublished };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var comment = new Comment
        {
            PostId = post.Id,
            AuthorId = author.Id,
            Content = "Original comment"
        };
        context.Comments.Add(comment);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var commentRepo = new GenericRepository<Comment>(context);

        var handler = new UpdateCommentHandler(commentRepo, uow);

        var command = new UpdateCommentCommand(
            CommentId: comment.Id,
            Content: "Anonymous edit attempt",
            CallerId: null
        );

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateComment_CommentDoesNotExist_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var commentRepo = new GenericRepository<Comment>(context);

        var handler = new UpdateCommentHandler(commentRepo, uow);

        var command = new UpdateCommentCommand(
            CommentId: 9999,
            Content: "Updated text",
            CallerId: 1
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateComment_DeletedComment_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var author = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
        context.Employees.Add(author);

        var post = new Post { Title = "Post", Content = "Content", PostType = "News", Status = NewsLifecycleManager.StatusPublished };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var comment = new Comment
        {
            PostId = post.Id,
            AuthorId = author.Id,
            Content = "Original text",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };
        context.Comments.Add(comment);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var commentRepo = new GenericRepository<Comment>(context);

        var handler = new UpdateCommentHandler(commentRepo, uow);

        var command = new UpdateCommentCommand(
            CommentId: comment.Id,
            Content: "Attempted edit on deleted comment",
            CallerId: author.Id
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateComment_ModeratedCommentByAdmin_ThrowsInvalidOperationException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var author = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
        context.Employees.Add(author);

        var post = new Post { Title = "Post", Content = "Content", PostType = "News", Status = NewsLifecycleManager.StatusPublished };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var comment = new Comment
        {
            PostId = post.Id,
            AuthorId = author.Id,
            Content = "Inappropriate comment",
            IsModerated = true,
            ModerationReason = "Violates guidelines"
        };
        context.Comments.Add(comment);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var commentRepo = new GenericRepository<Comment>(context);

        var handler = new UpdateCommentHandler(commentRepo, uow);

        var command = new UpdateCommentCommand(
            CommentId: comment.Id,
            Content: "Attempted edit on moderated comment",
            CallerId: author.Id
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("administrator", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task UpdateComment_EmptyContent_ThrowsValidationException(string? invalidContent)
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var commentRepo = new GenericRepository<Comment>(context);

        var handler = new UpdateCommentHandler(commentRepo, uow);

        var command = new UpdateCommentCommand(
            CommentId: 1,
            Content: invalidContent!,
            CallerId: 1
        );

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateComment_OversizedContent_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var commentRepo = new GenericRepository<Comment>(context);

        var handler = new UpdateCommentHandler(commentRepo, uow);

        var command = new UpdateCommentCommand(
            CommentId: 1,
            Content: new string('A', 2001),
            CallerId: 1
        );

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateComment_InvalidCommentId_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var commentRepo = new GenericRepository<Comment>(context);

        var handler = new UpdateCommentHandler(commentRepo, uow);

        var command = new UpdateCommentCommand(
            CommentId: 0,
            Content: "Valid content",
            CallerId: 1
        );

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateComment_ControllerEndpoint_Success_Returns200Ok()
    {
        var expectedComment = new CommentDto(
            Id: 55,
            PostId: 10,
            ParentCommentId: null,
            AuthorId: 7,
            AuthorName: "Sarah Connor",
            AuthorAvatar: null,
            Content: "Updated comment text",
            IsModerated: false,
            ModerationReason: null,
            LikesCount: 3,
            CreatedAt: DateTime.UtcNow.AddHours(-1),
            UpdatedAt: DateTime.UtcNow
        );

        var mediator = new FakeCommentsMediator(expectedComment);
        var controller = new NewsCommentsController(mediator);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "7")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var requestDto = new UpdateCommentDto("Updated comment text");

        var actionResult = await controller.UpdateComment(55, requestDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedDto = Assert.IsType<CommentDto>(okResult.Value);
        Assert.Equal(55, returnedDto.Id);
        Assert.Equal("Updated comment text", returnedDto.Content);
        Assert.NotNull(returnedDto.UpdatedAt);
    }

    [Fact]
    public async Task UpdateComment_ControllerEndpoint_Unauthorized_Returns403Forbidden()
    {
        var mediator = new FakeCommentsMediator(exceptionToThrow: new UnauthorizedAccessException("Only author can edit"));
        var controller = new NewsCommentsController(mediator);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var requestDto = new UpdateCommentDto("Unauthorized edit");

        var actionResult = await controller.UpdateComment(55, requestDto, CancellationToken.None);

        var forbiddenResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
    }

    [Fact]
    public async Task UpdateComment_ControllerEndpoint_NotFound_Returns404NotFound()
    {
        var mediator = new FakeCommentsMediator(exceptionToThrow: new KeyNotFoundException("Comment not found"));
        var controller = new NewsCommentsController(mediator);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "7")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var requestDto = new UpdateCommentDto("Edit non-existent");

        var actionResult = await controller.UpdateComment(999, requestDto, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task UpdateComment_ControllerEndpoint_Moderated_Returns400BadRequest()
    {
        var mediator = new FakeCommentsMediator(exceptionToThrow: new InvalidOperationException("Cannot edit moderated comment"));
        var controller = new NewsCommentsController(mediator);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "7")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var requestDto = new UpdateCommentDto("Edit moderated");

        var actionResult = await controller.UpdateComment(55, requestDto, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    private class FakeCommentsMediator : ISender
    {
        private readonly object? _result;
        private readonly Exception? _exceptionToThrow;

        public FakeCommentsMediator(object? result = null, Exception? exceptionToThrow = null)
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
