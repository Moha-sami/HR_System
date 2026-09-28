using Buy2.Api.Controllers;
using Buy2.Application.DTOs.News;
using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.News.GetNewsFeed;
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

public class GetNewsFeedQueryTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetNewsFeed_NonElevatedUser_OnlyReturnsPublishedAndReleasedPosts()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "HR", LastName = "Admin", Email = "hr@example.com" };
            context.Employees.Add(author);
            await context.SaveChangesAsync();

            context.Posts.AddRange(
                new Post
                {
                    AuthorId = author.Id,
                    Title = "Live Announcement",
                    Content = "Everyone is invited.",
                    Category = "General",
                    Status = "Published",
                    PostType = "News",
                    PublishedAt = DateTime.UtcNow.AddHours(-1)
                },
                new Post
                {
                    AuthorId = author.Id,
                    Title = "Secret Draft",
                    Content = "Upcoming reorganization.",
                    Category = "Strategy",
                    Status = "Draft",
                    PostType = "News"
                },
                new Post
                {
                    AuthorId = author.Id,
                    Title = "Future Scheduled",
                    Content = "Will be announced next week.",
                    Category = "Product",
                    Status = "Scheduled",
                    PostType = "News",
                    ScheduledFor = DateTime.UtcNow.AddDays(3)
                },
                new Post
                {
                    AuthorId = author.Id,
                    Title = "Released Scheduled",
                    Content = "Was scheduled for 10 mins ago.",
                    Category = "General",
                    Status = "Scheduled",
                    PostType = "News",
                    ScheduledFor = DateTime.UtcNow.AddMinutes(-10)
                }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var repo = new GenericRepository<Post>(context);
            var handler = new GetNewsFeedQueryHandler(repo);

            var query = new GetNewsFeedQuery(IsElevatedUser: false);
            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Equal(2, result.TotalCount);
            Assert.Contains(result.Items, p => p.Title == "Live Announcement");
            Assert.Contains(result.Items, p => p.Title == "Released Scheduled");
            Assert.DoesNotContain(result.Items, p => p.Title == "Secret Draft");
            Assert.DoesNotContain(result.Items, p => p.Title == "Future Scheduled");
        }
    }

    [Fact]
    public async Task GetNewsFeed_ElevatedUser_CanFilterByStatus()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Editor", LastName = "Chief", Email = "editor@example.com" };
            context.Employees.Add(author);
            await context.SaveChangesAsync();

            context.Posts.AddRange(
                new Post { AuthorId = author.Id, Title = "Draft 1", Content = "Content 1", Status = "Draft", PostType = "News" },
                new Post { AuthorId = author.Id, Title = "Published 1", Content = "Content 2", Status = "Published", PostType = "News" }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var repo = new GenericRepository<Post>(context);
            var handler = new GetNewsFeedQueryHandler(repo);

            var query = new GetNewsFeedQuery(IsElevatedUser: true, Status: "Draft");
            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Single(result.Items);
            Assert.Equal("Draft 1", result.Items[0].Title);
        }
    }

    [Fact]
    public async Task GetNewsFeed_SearchAndCategoryFilter_FiltersCorrectly()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "HR", LastName = "Team", Email = "hrteam@example.com" };
            context.Employees.Add(author);
            await context.SaveChangesAsync();

            context.Posts.AddRange(
                new Post { AuthorId = author.Id, Title = "Quarterly Results", Content = "Revenue grew by 20%", Category = "Finance", Status = "Published", PostType = "News" },
                new Post { AuthorId = author.Id, Title = "Holiday Party", Content = "Join us next Friday", Category = "Culture", Status = "Published", PostType = "News" }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var repo = new GenericRepository<Post>(context);
            var handler = new GetNewsFeedQueryHandler(repo);

            // Search by keyword
            var searchResult = await handler.Handle(new GetNewsFeedQuery(Search: "Revenue", IsElevatedUser: false), CancellationToken.None);
            Assert.Single(searchResult.Items);
            Assert.Equal("Quarterly Results", searchResult.Items[0].Title);

            // Filter by category
            var categoryResult = await handler.Handle(new GetNewsFeedQuery(Category: "Culture", IsElevatedUser: false), CancellationToken.None);
            Assert.Single(categoryResult.Items);
            Assert.Equal("Holiday Party", categoryResult.Items[0].Title);
        }
    }

    [Fact]
    public async Task GetNewsFeed_AggregatesLikesAndCommentsCount()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "User", LastName = "One", Email = "u1@example.com" };
            context.Employees.Add(emp);
            await context.SaveChangesAsync();

            var post = new Post
            {
                AuthorId = emp.Id,
                Title = "Engaging Post",
                Content = "Leave your thoughts below.",
                Status = "Published",
                PostType = "News"
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            context.Comments.AddRange(
                new Comment { PostId = post.Id, AuthorId = emp.Id, Content = "Great post!" },
                new Comment { PostId = post.Id, AuthorId = emp.Id, Content = "Deleted comment", IsDeleted = true }
            );

            context.Reactions.AddRange(
                new Reaction { PostId = post.Id, TargetType = "Post", TargetId = post.Id, EmployeeId = emp.Id, ReactionType = "Like" }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var repo = new GenericRepository<Post>(context);
            var handler = new GetNewsFeedQueryHandler(repo);

            var result = await handler.Handle(new GetNewsFeedQuery(IsElevatedUser: false), CancellationToken.None);

            Assert.Single(result.Items);
            Assert.Equal(1, result.Items[0].LikesCount);
            Assert.Equal(1, result.Items[0].CommentsCount); // Excludes deleted comment
        }
    }

    [Fact]
    public async Task NewsController_GetNewsFeed_DispatchesQueryCorrectly()
    {
        var fakeResult = new PaginatedListResult<NewsFeedSummaryDto>(
            new List<NewsFeedSummaryDto>
            {
                new NewsFeedSummaryDto(1, "Sample Post", "Summary", "General", "Published", 1, "Admin", null, null, null, DateTime.UtcNow, 5, 2, DateTime.UtcNow)
            },
            1, 1, 10, 1
        );

        var controller = new NewsController(new FakeNewsMediator(fakeResult));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }))
            }
        };

        var actionResult = await controller.GetNewsFeed(search: null, category: null, status: null);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<PaginatedListResult<NewsFeedSummaryDto>>(okResult.Value);

        Assert.Single(response.Items);
        Assert.Equal("Sample Post", response.Items[0].Title);
    }

    private class FakeNewsMediator : ISender
    {
        private readonly object _result;

        public FakeNewsMediator(object result)
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
