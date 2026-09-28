using Buy2.Api.Controllers;
using Buy2.Application.Features.News.DeleteNewsPost;
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

namespace Buy2.Domain.Tests.News;

public class DeleteNewsPostCommandTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task DeleteNewsPost_NonExistentPost_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var postRepo = new GenericRepository<Post>(context);
        var uow = new UnitOfWork(context);

        var handler = new DeleteNewsPostCommandHandler(postRepo, uow);
        var command = new DeleteNewsPostCommand(Id: 9999, IsElevatedUser: true);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteNewsPost_NonElevatedUser_ThrowsUnauthorizedAccessException()
    {
        var dbName = Guid.NewGuid().ToString();
        int postId;

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "HR", LastName = "Admin", Email = "hr@corp.com" };
            context.Employees.Add(author);
            await context.SaveChangesAsync();

            var post = new Post
            {
                AuthorId = author.Id,
                Title = "Announcement",
                Content = "Company picnic announcement",
                PostType = "News"
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();
            postId = post.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var postRepo = new GenericRepository<Post>(context);
            var uow = new UnitOfWork(context);
            var handler = new DeleteNewsPostCommandHandler(postRepo, uow);

            var command = new DeleteNewsPostCommand(Id: postId, IsElevatedUser: false, CurrentUserId: 42);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
            Assert.Contains("administrative authority", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task DeleteNewsPost_ElevatedUser_SoftDeletesPostAndSuppressesComments()
    {
        var dbName = Guid.NewGuid().ToString();
        int postId;

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Admin", LastName = "User", Email = "admin@corp.com" };
            var commenter = new Employee { FirstName = "John", LastName = "Doe", Email = "john@corp.com" };
            context.Employees.AddRange(author, commenter);
            await context.SaveChangesAsync();

            var post = new Post
            {
                AuthorId = author.Id,
                Title = "Outdated News",
                Content = "This is old information",
                PostType = "News",
                IsDeleted = false
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();
            postId = post.Id;

            var comment1 = new Comment
            {
                PostId = postId,
                AuthorId = commenter.Id,
                Content = "First comment",
                IsDeleted = false
            };
            var comment2 = new Comment
            {
                PostId = postId,
                AuthorId = commenter.Id,
                Content = "Second comment",
                IsDeleted = false
            };
            context.Comments.AddRange(comment1, comment2);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var postRepo = new GenericRepository<Post>(context);
            var uow = new UnitOfWork(context);
            var handler = new DeleteNewsPostCommandHandler(postRepo, uow);

            var command = new DeleteNewsPostCommand(Id: postId, IsElevatedUser: true, CurrentUserId: 1);
            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result);
        }

        // Verify with IgnoreQueryFilters to inspect raw soft-deleted records in DB
        using (var context = CreateDbContext(dbName))
        {
            var deletedPost = await context.Posts.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == postId);
            Assert.NotNull(deletedPost);
            Assert.True(deletedPost.IsDeleted);
            Assert.NotNull(deletedPost.DeletedAt);

            var comments = await context.Comments.IgnoreQueryFilters().Where(c => c.PostId == postId).ToListAsync();
            Assert.Equal(2, comments.Count);
            Assert.All(comments, c =>
            {
                Assert.True(c.IsDeleted);
                Assert.NotNull(c.DeletedAt);
            });

            // Default EF query with query filters should exclude both post and comments
            var activePost = await context.Posts.FirstOrDefaultAsync(p => p.Id == postId);
            Assert.Null(activePost);

            var activeComments = await context.Comments.Where(c => c.PostId == postId).ToListAsync();
            Assert.Empty(activeComments);
        }
    }

    [Fact]
    public async Task NewsController_DeleteNewsPost_ReturnsNoContent_WhenAdmin()
    {
        var mediator = new FakeDeleteMediator(result: true);
        var controller = new NewsController(mediator)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "1"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "TestAuth"))
                }
            }
        };

        var actionResult = await controller.DeleteNewsPost(123, CancellationToken.None);

        var noContentResult = Assert.IsType<NoContentResult>(actionResult);
        Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
    }

    [Fact]
    public async Task NewsController_DeleteNewsPost_ReturnsForbidden_WhenUnauthorizedAccessExceptionThrown()
    {
        var mediator = new FakeDeleteMediator(exceptionToThrow: new UnauthorizedAccessException("User does not have administrative authority."));
        var controller = new NewsController(mediator)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "5"),
                        new Claim(ClaimTypes.Role, "Employee")
                    }, "TestAuth"))
                }
            }
        };

        var actionResult = await controller.DeleteNewsPost(123, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task NewsController_DeleteNewsPost_ReturnsNotFound_WhenPostDoesNotExist()
    {
        var mediator = new FakeDeleteMediator(exceptionToThrow: new KeyNotFoundException("News post not found."));
        var controller = new NewsController(mediator)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "1"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "TestAuth"))
                }
            }
        };

        var actionResult = await controller.DeleteNewsPost(999, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    private class FakeDeleteMediator : ISender
    {
        private readonly object? _result;
        private readonly Exception? _exceptionToThrow;

        public FakeDeleteMediator(object? result = null, Exception? exceptionToThrow = null)
        {
            _result = result;
            _exceptionToThrow = exceptionToThrow;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (_exceptionToThrow != null)
            {
                throw _exceptionToThrow;
            }
            return Task.FromResult((TResponse)_result!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            if (_exceptionToThrow != null)
            {
                throw _exceptionToThrow;
            }
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            if (_exceptionToThrow != null)
            {
                throw _exceptionToThrow;
            }
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
