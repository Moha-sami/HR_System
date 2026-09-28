using Buy2.Api.Controllers;
using Buy2.Application.DTOs.News;
using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.News.Common;
using Buy2.Application.Features.News.GetPostComments;
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

public class GetPostCommentsQueryTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetPostComments_NonExistentPost_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var postRepo = new GenericRepository<Post>(context);
        var commentRepo = new GenericRepository<Comment>(context);

        var handler = new GetPostCommentsQueryHandler(postRepo, commentRepo);
        var query = new GetPostCommentsQuery(PostId: 9999);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task GetPostComments_ReturnsThreadedHierarchyWithRepliesAndReactions()
    {
        var dbName = Guid.NewGuid().ToString();
        int postId;
        int callerId;

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Post", LastName = "Author", Email = "author@corp.com" };
            var user1 = new Employee { FirstName = "Jane", LastName = "Smith", Email = "jane@corp.com" };
            var user2 = new Employee { FirstName = "Bob", LastName = "Jones", Email = "bob@corp.com" };
            context.Employees.AddRange(author, user1, user2);
            await context.SaveChangesAsync();

            callerId = user1.Id;

            var post = new Post
            {
                AuthorId = author.Id,
                Title = "Quarterly Update",
                Content = "Great quarter everybody!",
                PostType = "News",
                Status = "Published"
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();
            postId = post.Id;

            var parentComment = new Comment
            {
                PostId = postId,
                AuthorId = user1.Id,
                Content = "Parent discussion comment",
                CreatedAt = DateTime.UtcNow.AddMinutes(-20)
            };
            context.Comments.Add(parentComment);
            await context.SaveChangesAsync();

            var replyComment = new Comment
            {
                PostId = postId,
                ParentCommentId = parentComment.Id,
                AuthorId = user2.Id,
                Content = "Direct reply to Jane",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            context.Comments.Add(replyComment);
            await context.SaveChangesAsync();

            var reaction1 = new Reaction
            {
                CommentId = parentComment.Id,
                TargetType = "Comment",
                TargetId = parentComment.Id,
                EmployeeId = user1.Id,
                ReactionType = "Like"
            };
            var reaction2 = new Reaction
            {
                CommentId = parentComment.Id,
                TargetType = "Comment",
                TargetId = parentComment.Id,
                EmployeeId = user2.Id,
                ReactionType = "Heart"
            };
            context.Reactions.AddRange(reaction1, reaction2);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var postRepo = new GenericRepository<Post>(context);
            var commentRepo = new GenericRepository<Comment>(context);
            var handler = new GetPostCommentsQueryHandler(postRepo, commentRepo);

            var query = new GetPostCommentsQuery(PostId: postId, CurrentUserId: callerId, PageNumber: 1, PageSize: 10);
            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            var thread = Assert.Single(result.Items);

            Assert.Equal("Jane Smith", thread.AuthorName);
            Assert.Equal("Parent discussion comment", thread.Content);
            Assert.Equal(1, thread.RepliesCount);
            Assert.Single(thread.Replies);

            var reply = thread.Replies[0];
            Assert.Equal("Bob Jones", reply.AuthorName);
            Assert.Equal("Direct reply to Jane", reply.Content);
            Assert.Equal(thread.Id, reply.ParentCommentId);

            // Reaction tally & caller state check
            Assert.Equal(1, thread.LikesCount);
            Assert.Equal("Like", thread.UserReaction);
            Assert.Equal(2, thread.ReactionBreakdown.Count);
            Assert.Equal(1, thread.ReactionBreakdown["Like"]);
            Assert.Equal(1, thread.ReactionBreakdown["Heart"]);
        }
    }

    [Fact]
    public async Task GetPostComments_ModeratedComment_MasksWithTombstone()
    {
        var dbName = Guid.NewGuid().ToString();
        int postId;

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Post", LastName = "Author", Email = "author@corp.com" };
            var commenter = new Employee { FirstName = "Bad", LastName = "Actor", Email = "bad@corp.com" };
            context.Employees.AddRange(author, commenter);
            await context.SaveChangesAsync();

            var post = new Post { AuthorId = author.Id, Title = "Guidelines", Content = "Be nice", PostType = "News" };
            context.Posts.Add(post);
            await context.SaveChangesAsync();
            postId = post.Id;

            var moderatedComment = new Comment
            {
                PostId = postId,
                AuthorId = commenter.Id,
                Content = "Offensive text",
                IsModerated = true,
                ModerationReason = "Policy violation",
                CreatedAt = DateTime.UtcNow
            };
            context.Comments.Add(moderatedComment);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var postRepo = new GenericRepository<Post>(context);
            var commentRepo = new GenericRepository<Comment>(context);
            var handler = new GetPostCommentsQueryHandler(postRepo, commentRepo);

            var query = new GetPostCommentsQuery(PostId: postId);
            var result = await handler.Handle(query, CancellationToken.None);

            var thread = Assert.Single(result.Items);
            Assert.True(thread.IsModerated);
            Assert.Equal("Policy violation", thread.ModerationReason);
            Assert.Equal("This comment has been removed by admin", thread.Content);
        }
    }

    [Fact]
    public async Task NewsCommentsController_GetPostComments_ReturnsOk()
    {
        var sampleResult = new PaginatedListResult<CommentThreadDto>(
            new List<CommentThreadDto>(),
            0,
            1,
            10,
            0
        );

        var mediator = new FakeCommentsMediator(sampleResult);
        var controller = new NewsCommentsController(mediator)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "42")
                    }, "TestAuth"))
                }
            }
        };

        var actionResult = await controller.GetPostComments(100, 1, 10, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var paged = Assert.IsType<PaginatedListResult<CommentThreadDto>>(okResult.Value);
        Assert.Empty(paged.Items);
    }

    [Fact]
    public async Task NewsCommentsController_GetPostComments_ReturnsNotFound_WhenPostDoesNotExist()
    {
        var mediator = new FakeCommentsMediator(exceptionToThrow: new KeyNotFoundException("Post not found"));
        var controller = new NewsCommentsController(mediator)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        var actionResult = await controller.GetPostComments(999, 1, 10, CancellationToken.None);
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
