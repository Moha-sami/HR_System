using Buy2.Api.Controllers;
using Buy2.Application.DTOs.News;
using Buy2.Application.Features.News.GetNewsPostDetail;
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

public class GetNewsPostDetailTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetNewsPostDetail_ExistingPublishedPost_ReturnsFullDetailWithEngagement()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Editor", LastName = "InChief", Email = "editor@press.com" };
            var reader = new Employee { FirstName = "Active", LastName = "Reader", Email = "reader@press.com" };
            context.Employees.AddRange(author, reader);
            await context.SaveChangesAsync();

            var post = new Post
            {
                AuthorId = author.Id,
                Title = "Major Milestone",
                Content = "Here is the full story about our achievement.",
                Category = "Milestone",
                Status = "Published",
                PostType = "News",
                MediaUrl = "https://cdn.example.com/hero.jpg",
                PublishedAt = DateTime.UtcNow
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            context.Comments.AddRange(
                new Comment { PostId = post.Id, AuthorId = reader.Id, Content = "Congrats!" },
                new Comment { PostId = post.Id, AuthorId = reader.Id, Content = "Deleted comment", IsDeleted = true }
            );

            context.Reactions.AddRange(
                new Reaction { PostId = post.Id, TargetType = "Post", TargetId = post.Id, EmployeeId = reader.Id, ReactionType = "Celebrate" },
                new Reaction { PostId = post.Id, TargetType = "Post", TargetId = post.Id, EmployeeId = author.Id, ReactionType = "Like" }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var repo = new GenericRepository<Post>(context);
            var handler = new GetNewsPostDetailQueryHandler(repo);

            var reader = await context.Employees.FirstAsync(e => e.Email == "reader@press.com");
            var post = await context.Posts.FirstAsync();

            var query = new GetNewsPostDetailQuery(Id: post.Id, CurrentUserId: reader.Id, IsElevatedUser: false);
            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Major Milestone", result.Title);
            Assert.Equal("https://cdn.example.com/hero.jpg", result.MediaUrl);
            Assert.Equal("Editor InChief", result.AuthorName);
            Assert.Equal(1, result.CommentsCount); // Excludes deleted
            Assert.Equal(1, result.LikesCount);
            Assert.Equal("Celebrate", result.UserReaction);
            Assert.Equal(2, result.ReactionBreakdown.Count);
            Assert.Equal(1, result.ReactionBreakdown["Celebrate"]);
            Assert.Equal(1, result.ReactionBreakdown["Like"]);
        }
    }

    [Fact]
    public async Task GetNewsPostDetail_NonElevatedUser_DraftPost_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Author", LastName = "User", Email = "author@corp.com" };
            context.Employees.Add(author);
            await context.SaveChangesAsync();

            var post = new Post
            {
                AuthorId = author.Id,
                Title = "Draft Post",
                Content = "Unreleased draft",
                Status = "Draft",
                PostType = "News"
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var repo = new GenericRepository<Post>(context);
            var handler = new GetNewsPostDetailQueryHandler(repo);
            var post = await context.Posts.FirstAsync();

            var query = new GetNewsPostDetailQuery(Id: post.Id, CurrentUserId: null, IsElevatedUser: false);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
        }
    }

    [Fact]
    public async Task GetNewsPostDetail_ElevatedUser_CanViewDraft()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Author", LastName = "User", Email = "author@corp.com" };
            context.Employees.Add(author);
            await context.SaveChangesAsync();

            var post = new Post
            {
                AuthorId = author.Id,
                Title = "Draft Post",
                Content = "Draft body",
                Status = "Draft",
                PostType = "News"
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var repo = new GenericRepository<Post>(context);
            var handler = new GetNewsPostDetailQueryHandler(repo);
            var post = await context.Posts.FirstAsync();

            var query = new GetNewsPostDetailQuery(Id: post.Id, CurrentUserId: null, IsElevatedUser: true);
            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Draft Post", result.Title);
            Assert.Equal("Draft", result.Status);
        }
    }

    [Fact]
    public async Task NewsController_GetNewsPostDetail_ReturnsOk()
    {
        var fakeDetail = new NewsPostDetailDto(
            1, "Detailed Title", "Full Body", "General", "Published", 1, "Author", null, null, null, DateTime.UtcNow,
            10, 2, new Dictionary<string, int> { { "Like", 10 } }, "Like", DateTime.UtcNow, null
        );

        var controller = new NewsController(new FakeDetailMediator(fakeDetail));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "1") }))
            }
        };

        var actionResult = await controller.GetNewsPostDetail(1, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<NewsPostDetailDto>(okResult.Value);

        Assert.Equal("Detailed Title", response.Title);
    }

    private class FakeDetailMediator : ISender
    {
        private readonly object _result;

        public FakeDetailMediator(object result)
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
