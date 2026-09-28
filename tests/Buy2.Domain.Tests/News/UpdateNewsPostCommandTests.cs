using Buy2.Api.Controllers;
using Buy2.Application.DTOs.News;
using Buy2.Application.Features.News.UpdateNewsPost;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Buy2.Domain.Tests.News;

public class UpdateNewsPostCommandTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task UpdateNewsPost_ExistingPost_UpdatesFieldsAndSetsUpdatedAt()
    {
        var dbName = Guid.NewGuid().ToString();
        int postId;

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Editor", LastName = "User", Email = "editor@corp.com" };
            context.Employees.Add(author);
            await context.SaveChangesAsync();

            var post = new Post
            {
                AuthorId = author.Id,
                Title = "Old Title",
                Content = "Old Content",
                Category = "General",
                Status = "Draft",
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
            var validator = new UpdateNewsPostCommandValidator();

            var handler = new UpdateNewsPostCommandHandler(postRepo, uow, validator);

            var command = new UpdateNewsPostCommand(
                Id: postId,
                Title: "New Updated Title",
                Content: "New Updated Content",
                Category: "Engineering",
                Status: "Published"
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("New Updated Title", result.Title);
            Assert.Equal("Published", result.Status);

            var updated = await context.Posts.FindAsync(postId);
            Assert.NotNull(updated);
            Assert.Equal("New Updated Title", updated.Title);
            Assert.Equal("New Updated Content", updated.Content);
            Assert.Equal("Engineering", updated.Category);
            Assert.Equal("Published", updated.Status);
            Assert.NotNull(updated.PublishedAt);
            Assert.NotNull(updated.UpdatedAt);
        }
    }

    [Fact]
    public async Task UpdateNewsPost_NonExistentPost_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var postRepo = new GenericRepository<Post>(context);
            var uow = new UnitOfWork(context);
            var validator = new UpdateNewsPostCommandValidator();

            var handler = new UpdateNewsPostCommandHandler(postRepo, uow, validator);

            var command = new UpdateNewsPostCommand(
                Id: 9999,
                Title: "Title",
                Content: "Content",
                Category: "General",
                Status: "Draft"
            );

            await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
        }
    }

    [Fact]
    public async Task UpdateNewsPost_InvalidStatus_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var postRepo = new GenericRepository<Post>(context);
            var uow = new UnitOfWork(context);
            var validator = new UpdateNewsPostCommandValidator();

            var handler = new UpdateNewsPostCommandHandler(postRepo, uow, validator);

            var command = new UpdateNewsPostCommand(
                Id: 1,
                Title: "Title",
                Content: "Content",
                Category: "General",
                Status: "NonExistentStatus"
            );

            await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        }
    }

    [Fact]
    public async Task NewsController_UpdateNewsPost_ReturnsOkResult()
    {
        var fakeResponse = new NewsPostResponseDto(1, "Updated Title", "Published", "Updated successfully");
        var fakeMediator = new FakeUpdateMediator(fakeResponse);

        var controller = new NewsController(fakeMediator)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "1") }))
                }
            }
        };

        var model = new UpdateNewsPostModel
        {
            Title = "Updated Title",
            Content = "Updated Content",
            Category = "General",
            Status = "Published"
        };

        var actionResult = await controller.UpdateNewsPost(1, model, null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<NewsPostResponseDto>(okResult.Value);

        Assert.Equal("Updated Title", response.Title);
        Assert.Equal("Published", response.Status);
    }

    private class FakeUpdateMediator : ISender
    {
        private readonly object _result;

        public FakeUpdateMediator(object result)
        {
            _result = result;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult((TResponse)_result);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<object?>(_result);
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
