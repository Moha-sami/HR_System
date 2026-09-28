using Buy2.Api.Controllers;
using Buy2.Application.DTOs.News;
using Buy2.Application.Features.News.Commands.DeleteComment;
using Buy2.Application.Features.News.Common;
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

namespace Buy2.Domain.Tests.News;

public class DeleteCommentCommandTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task DeleteComment_AuthorWithNoReplies_SoftDeletesCommentAndDecrementsCount()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
            context.Employees.Add(author);

            var post = new Post
            {
                Title = "Published News",
                Content = "Post content",
                PostType = "News",
                Status = NewsLifecycleManager.StatusPublished,
                CommentsCount = 1
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var comment = new Comment
            {
                PostId = post.Id,
                AuthorId = author.Id,
                Content = "Comment without replies",
                IsDeleted = false
            };
            context.Comments.Add(comment);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var commentRepo = new GenericRepository<Comment>(context);
            var postRepo = new GenericRepository<Post>(context);

            var handler = new DeleteCommentHandler(commentRepo, postRepo, uow);

            var command = new DeleteCommentCommand(
                CommentId: comment.Id,
                CallerId: author.Id,
                IsElevatedUser: false
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result);
            Assert.True(comment.IsDeleted);
            Assert.NotNull(comment.DeletedAt);
            Assert.Equal(0, post.CommentsCount);
        }
    }

    [Fact]
    public async Task DeleteComment_AuthorWithChildReplies_TombstonesContentAndDecrementsCount()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
            var replier = new Employee { FirstName = "Bob", LastName = "Jones", Email = "bob@example.com" };
            context.Employees.AddRange(author, replier);

            var post = new Post
            {
                Title = "Published News",
                Content = "Post content",
                PostType = "News",
                Status = NewsLifecycleManager.StatusPublished,
                CommentsCount = 2
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var parentComment = new Comment
            {
                PostId = post.Id,
                AuthorId = author.Id,
                Content = "Original comment with replies",
                IsDeleted = false
            };
            context.Comments.Add(parentComment);
            await context.SaveChangesAsync();

            var childReply = new Comment
            {
                PostId = post.Id,
                AuthorId = replier.Id,
                ParentCommentId = parentComment.Id,
                Content = "A nested reply",
                IsDeleted = false
            };
            context.Comments.Add(childReply);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var commentRepo = new GenericRepository<Comment>(context);
            var postRepo = new GenericRepository<Post>(context);

            var handler = new DeleteCommentHandler(commentRepo, postRepo, uow);

            var command = new DeleteCommentCommand(
                CommentId: parentComment.Id,
                CallerId: author.Id,
                IsElevatedUser: false
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result);
            Assert.False(parentComment.IsDeleted); // Preserved for continuity
            Assert.Equal(SocialEngagementManager.TombstoneDeletedComment, parentComment.Content);
            Assert.NotNull(parentComment.UpdatedAt);
            Assert.Equal(1, post.CommentsCount);
        }
    }

    [Fact]
    public async Task DeleteComment_AdminOrModerator_SetsAdminTombstoneFlagAndContent()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Regular", LastName = "User", Email = "user@example.com" };
            var admin = new Employee { FirstName = "Admin", LastName = "Officer", Email = "admin@example.com" };
            context.Employees.AddRange(author, admin);

            var post = new Post
            {
                Title = "Published News",
                Content = "Post content",
                PostType = "News",
                Status = NewsLifecycleManager.StatusPublished,
                CommentsCount = 1
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var comment = new Comment
            {
                PostId = post.Id,
                AuthorId = author.Id,
                Content = "Offensive comment",
                IsDeleted = false
            };
            context.Comments.Add(comment);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var commentRepo = new GenericRepository<Comment>(context);
            var postRepo = new GenericRepository<Post>(context);

            var handler = new DeleteCommentHandler(commentRepo, postRepo, uow);

            var command = new DeleteCommentCommand(
                CommentId: comment.Id,
                CallerId: admin.Id,
                IsElevatedUser: true,
                ModerationReason: "Community policy breach"
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result);
            Assert.True(comment.IsModerated);
            Assert.Equal("Community policy breach", comment.ModerationReason);
            Assert.Equal(SocialEngagementManager.TombstoneModeratedComment, comment.Content);
            Assert.Equal(0, post.CommentsCount);
        }
    }

    [Fact]
    public async Task DeleteComment_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var author = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
        var otherUser = new Employee { FirstName = "Other", LastName = "User", Email = "other@example.com" };
        context.Employees.AddRange(author, otherUser);

        var post = new Post { Title = "Post", Content = "Content", PostType = "News", Status = NewsLifecycleManager.StatusPublished };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var comment = new Comment
        {
            PostId = post.Id,
            AuthorId = author.Id,
            Content = "Author comment",
            IsDeleted = false
        };
        context.Comments.Add(comment);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var postRepo = new GenericRepository<Post>(context);

        var handler = new DeleteCommentHandler(commentRepo, postRepo, uow);

        var command = new DeleteCommentCommand(
            CommentId: comment.Id,
            CallerId: otherUser.Id,
            IsElevatedUser: false
        );

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteComment_NonExistentComment_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var postRepo = new GenericRepository<Post>(context);

        var handler = new DeleteCommentHandler(commentRepo, postRepo, uow);

        var command = new DeleteCommentCommand(
            CommentId: 9999,
            CallerId: 1,
            IsElevatedUser: true
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteComment_AlreadyDeletedComment_ThrowsKeyNotFoundException()
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
            Content = "Deleted comment",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };
        context.Comments.Add(comment);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var postRepo = new GenericRepository<Post>(context);

        var handler = new DeleteCommentHandler(commentRepo, postRepo, uow);

        var command = new DeleteCommentCommand(
            CommentId: comment.Id,
            CallerId: author.Id,
            IsElevatedUser: false
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteComment_AuthorAttemptsToDeleteModeratedComment_ThrowsInvalidOperationException()
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
            Content = SocialEngagementManager.TombstoneModeratedComment,
            IsModerated = true,
            ModerationReason = "Policy violation"
        };
        context.Comments.Add(comment);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var postRepo = new GenericRepository<Post>(context);

        var handler = new DeleteCommentHandler(commentRepo, postRepo, uow);

        var command = new DeleteCommentCommand(
            CommentId: comment.Id,
            CallerId: author.Id,
            IsElevatedUser: false
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteComment_AdminAttemptsToModerateAlreadyModeratedComment_ThrowsInvalidOperationException()
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
            Content = SocialEngagementManager.TombstoneModeratedComment,
            IsModerated = true,
            ModerationReason = "Policy violation"
        };
        context.Comments.Add(comment);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var postRepo = new GenericRepository<Post>(context);

        var handler = new DeleteCommentHandler(commentRepo, postRepo, uow);

        var command = new DeleteCommentCommand(
            CommentId: comment.Id,
            CallerId: 99,
            IsElevatedUser: true
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteComment_AlreadyTombstonedDeletedComment_ThrowsInvalidOperationException()
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
            Content = SocialEngagementManager.TombstoneDeletedComment,
            IsDeleted = false
        };
        context.Comments.Add(comment);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var postRepo = new GenericRepository<Post>(context);

        var handler = new DeleteCommentHandler(commentRepo, postRepo, uow);

        var command = new DeleteCommentCommand(
            CommentId: comment.Id,
            CallerId: author.Id,
            IsElevatedUser: false
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteComment_InvalidCommentId_ThrowsArgumentException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var commentRepo = new GenericRepository<Comment>(context);
        var postRepo = new GenericRepository<Post>(context);

        var handler = new DeleteCommentHandler(commentRepo, postRepo, uow);

        var command = new DeleteCommentCommand(
            CommentId: 0,
            CallerId: 1,
            IsElevatedUser: false
        );

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteComment_ControllerEndpoint_Author_Returns200Ok()
    {
        var mediator = new FakeCommentsMediator(result: true);
        var controller = new NewsCommentsController(mediator);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "7")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var actionResult = await controller.DeleteComment(42, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_ControllerEndpoint_Admin_Returns200Ok()
    {
        var mediator = new FakeCommentsMediator(result: true);
        var controller = new NewsCommentsController(mediator);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var actionResult = await controller.DeleteComment(42, "Inappropriate content", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_ControllerEndpoint_Unauthorized_Returns403Forbidden()
    {
        var mediator = new FakeCommentsMediator(exceptionToThrow: new UnauthorizedAccessException("Forbidden"));
        var controller = new NewsCommentsController(mediator);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var actionResult = await controller.DeleteComment(42, null, CancellationToken.None);

        var forbiddenResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_ControllerEndpoint_NotFound_Returns404NotFound()
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

        var actionResult = await controller.DeleteComment(999, null, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
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
