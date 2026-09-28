using Buy2.Api.Controllers;
using Buy2.Application.DTOs.News;
using Buy2.Application.Features.News.Commands.CreateComment;
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

public class CreateCommentCommandTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task CreateComment_ValidTopLevelComment_CreatesCommentAndIncrementsPostCounter()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
            context.Employees.Add(author);

            var post = new Post
            {
                Title = "Published News",
                Content = "Content of news",
                PostType = "News",
                Status = NewsLifecycleManager.StatusPublished,
                CommentsCount = 0
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var postRepo = new GenericRepository<Post>(context);
            var commentRepo = new GenericRepository<Comment>(context);
            var empRepo = new GenericRepository<Employee>(context);
            var validator = new CreateCommentValidator();

            var handler = new CreateCommentHandler(postRepo, commentRepo, empRepo, uow, validator);

            var command = new CreateCommentCommand(
                PostId: post.Id,
                Content: "Insightful news post, thanks for sharing!",
                ParentCommentId: null,
                AuthorId: author.Id
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal(post.Id, result.PostId);
            Assert.Null(result.ParentCommentId);
            Assert.Equal(author.Id, result.AuthorId);
            Assert.Equal("John Doe", result.AuthorName);
            Assert.Equal("Insightful news post, thanks for sharing!", result.Content);
            Assert.False(result.IsModerated);
            Assert.Equal(0, result.LikesCount);
        }

        using (var context = CreateDbContext(dbName))
        {
            var updatedPost = await context.Posts.FirstAsync();
            Assert.Equal(1, updatedPost.CommentsCount);

            var savedComment = await context.Comments.FirstAsync();
            Assert.Equal("Insightful news post, thanks for sharing!", savedComment.Content);
            Assert.Null(savedComment.ParentCommentId);
            Assert.False(savedComment.IsDeleted);
        }
    }

    [Fact]
    public async Task CreateComment_ValidThreadedReply_AssociatesParentCommentAndIncrementsCounter()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
            var replier = new Employee { FirstName = "Bob", LastName = "Jones", Email = "bob@example.com" };
            context.Employees.AddRange(author, replier);

            var post = new Post
            {
                Title = "Scheduled Live Post",
                Content = "Live content",
                PostType = "News",
                Status = NewsLifecycleManager.StatusScheduled,
                ScheduledFor = DateTime.UtcNow.AddMinutes(-5),
                CommentsCount = 1
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var parentComment = new Comment
            {
                PostId = post.Id,
                AuthorId = author.Id,
                Content = "Top level comment",
                CreatedAt = DateTime.UtcNow.AddMinutes(-2)
            };
            context.Comments.Add(parentComment);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var postRepo = new GenericRepository<Post>(context);
            var commentRepo = new GenericRepository<Comment>(context);
            var empRepo = new GenericRepository<Employee>(context);

            var handler = new CreateCommentHandler(postRepo, commentRepo, empRepo, uow);

            var command = new CreateCommentCommand(
                PostId: post.Id,
                Content: "This is a threaded reply!",
                ParentCommentId: parentComment.Id,
                AuthorId: replier.Id
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(parentComment.Id, result.ParentCommentId);
            Assert.Equal(replier.Id, result.AuthorId);
            Assert.Equal("Bob Jones", result.AuthorName);
        }

        using (var context = CreateDbContext(dbName))
        {
            var updatedPost = await context.Posts.FirstAsync();
            Assert.Equal(2, updatedPost.CommentsCount);

            var reply = await context.Comments.FirstOrDefaultAsync(c => c.ParentCommentId != null);
            Assert.NotNull(reply);
            Assert.Equal("This is a threaded reply!", reply.Content);
        }
    }

    [Fact]
    public async Task CreateComment_ParentCommentDoesNotExist_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var author = new Employee { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        context.Employees.Add(author);

        var post = new Post
        {
            Title = "News",
            Content = "Body",
            PostType = "News",
            Status = NewsLifecycleManager.StatusPublished
        };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var postRepo = new GenericRepository<Post>(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var handler = new CreateCommentHandler(postRepo, commentRepo, empRepo, uow);

        var command = new CreateCommentCommand(
            PostId: post.Id,
            Content: "Reply to ghost comment",
            ParentCommentId: 9999,
            AuthorId: author.Id
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateComment_ParentCommentBelongsToDifferentPost_ThrowsInvalidOperationException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var author = new Employee { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        context.Employees.Add(author);

        var post1 = new Post { Title = "Post 1", Content = "Body 1", PostType = "News", Status = NewsLifecycleManager.StatusPublished };
        var post2 = new Post { Title = "Post 2", Content = "Body 2", PostType = "News", Status = NewsLifecycleManager.StatusPublished };
        context.Posts.AddRange(post1, post2);
        await context.SaveChangesAsync();

        var commentOnPost2 = new Comment
        {
            PostId = post2.Id,
            AuthorId = author.Id,
            Content = "Belongs to post 2"
        };
        context.Comments.Add(commentOnPost2);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var postRepo = new GenericRepository<Post>(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var handler = new CreateCommentHandler(postRepo, commentRepo, empRepo, uow);

        var command = new CreateCommentCommand(
            PostId: post1.Id,
            Content: "Cross-post reply attempt",
            ParentCommentId: commentOnPost2.Id,
            AuthorId: author.Id
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateComment_NonExistentPost_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var postRepo = new GenericRepository<Post>(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var handler = new CreateCommentHandler(postRepo, commentRepo, empRepo, uow);

        var command = new CreateCommentCommand(
            PostId: 9999,
            Content: "Some comment"
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateComment_DraftPost_ThrowsInvalidOperationException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var author = new Employee { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        context.Employees.Add(author);

        var post = new Post
        {
            Title = "Draft News",
            Content = "Draft body",
            PostType = "News",
            Status = NewsLifecycleManager.StatusDraft
        };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var postRepo = new GenericRepository<Post>(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var handler = new CreateCommentHandler(postRepo, commentRepo, empRepo, uow);

        var command = new CreateCommentCommand(
            PostId: post.Id,
            Content: "Comment on draft",
            AuthorId: author.Id
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateComment_DeletedPost_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var author = new Employee { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        context.Employees.Add(author);

        var post = new Post
        {
            Title = "Deleted News",
            Content = "Deleted body",
            PostType = "News",
            Status = NewsLifecycleManager.StatusPublished,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var postRepo = new GenericRepository<Post>(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var handler = new CreateCommentHandler(postRepo, commentRepo, empRepo, uow);

        var command = new CreateCommentCommand(
            PostId: post.Id,
            Content: "Comment on deleted post",
            AuthorId: author.Id
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task CreateComment_EmptyContent_ThrowsValidationException(string? invalidContent)
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var postRepo = new GenericRepository<Post>(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var handler = new CreateCommentHandler(postRepo, commentRepo, empRepo, uow);

        var command = new CreateCommentCommand(
            PostId: 1,
            Content: invalidContent!
        );

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateComment_OversizedContent_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var postRepo = new GenericRepository<Post>(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var handler = new CreateCommentHandler(postRepo, commentRepo, empRepo, uow);

        var command = new CreateCommentCommand(
            PostId: 1,
            Content: new string('A', 2001)
        );

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateComment_InvalidPostId_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var postRepo = new GenericRepository<Post>(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var handler = new CreateCommentHandler(postRepo, commentRepo, empRepo, uow);

        var command = new CreateCommentCommand(
            PostId: 0,
            Content: "Valid comment text"
        );

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateComment_InvalidParentCommentId_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var postRepo = new GenericRepository<Post>(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var handler = new CreateCommentHandler(postRepo, commentRepo, empRepo, uow);

        var command = new CreateCommentCommand(
            PostId: 1,
            Content: "Valid comment text",
            ParentCommentId: -5
        );

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateComment_FallbackDefaultAuthor_WhenAuthorIdNotProvided()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var defaultAuthor = new Employee { FirstName = "Default", LastName = "Employee", Email = "default@example.com" };
            context.Employees.Add(defaultAuthor);

            var post = new Post
            {
                Title = "Published News",
                Content = "Post content",
                PostType = "News",
                Status = NewsLifecycleManager.StatusPublished
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var postRepo = new GenericRepository<Post>(context);
            var commentRepo = new GenericRepository<Comment>(context);
            var empRepo = new GenericRepository<Employee>(context);

            var handler = new CreateCommentHandler(postRepo, commentRepo, empRepo, uow);

            var command = new CreateCommentCommand(
                PostId: post.Id,
                Content: "Comment with fallback author",
                AuthorId: null
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal(defaultAuthor.Id, result.AuthorId);
            Assert.Equal("Default Employee", result.AuthorName);
        }
    }

    [Fact]
    public async Task CreateComment_ControllerEndpoint_Returns201Created()
    {
        var expectedComment = new CommentDto(
            Id: 42,
            PostId: 10,
            ParentCommentId: null,
            AuthorId: 7,
            AuthorName: "Sarah Connor",
            AuthorAvatar: null,
            Content: "Test comment from controller",
            IsModerated: false,
            ModerationReason: null,
            LikesCount: 0,
            CreatedAt: DateTime.UtcNow
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

        var requestDto = new CreateCommentDto(
            Content: "Test comment from controller"
        );

        var actionResult = await controller.CreateComment(10, requestDto, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        var returnedDto = Assert.IsType<CommentDto>(objectResult.Value);
        Assert.Equal(42, returnedDto.Id);
        Assert.Equal("Sarah Connor", returnedDto.AuthorName);
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
