using Buy2.Api.Controllers;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.News;
using Buy2.Application.Features.News.CreateNewsPost;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Buy2.Domain.Tests.News;

public class CreateNewsPostCommandTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task CreateNewsPost_PublishedPost_SetsPublishedAtAndPersists()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Sarah", LastName = "Connor", Email = "sarah@rebel.org" };
            context.Employees.Add(author);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var postRepo = new GenericRepository<Post>(context);
            var empRepo = new GenericRepository<Employee>(context);
            var validator = new CreateNewsPostCommandValidator();

            var handler = new CreateNewsPostCommandHandler(postRepo, empRepo, uow, validator);

            var command = new CreateNewsPostCommand(
                Title: "Company Milestone Reached",
                Content: "We achieved 100k active users today!",
                Category: "Milestone",
                Status: "Published",
                AuthorId: author.Id
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Company Milestone Reached", result.Title);
            Assert.Equal("Published", result.Status);

            var persisted = await context.Posts.FirstOrDefaultAsync(p => p.Id == result.Id);
            Assert.NotNull(persisted);
            Assert.NotNull(persisted.PublishedAt);
            Assert.Equal(author.Id, persisted.AuthorId);
        }
    }

    [Fact]
    public async Task CreateNewsPost_ScheduledPost_WithFutureDate_PersistsScheduledDate()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "John", LastName = "Connor", Email = "john@rebel.org" };
            context.Employees.Add(author);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var postRepo = new GenericRepository<Post>(context);
            var empRepo = new GenericRepository<Employee>(context);
            var validator = new CreateNewsPostCommandValidator();

            var handler = new CreateNewsPostCommandHandler(postRepo, empRepo, uow, validator);
            var futureDate = DateTime.UtcNow.AddDays(2);

            var command = new CreateNewsPostCommand(
                Title: "Future Product Launch",
                Content: "Coming soon next week.",
                Category: "Product",
                Status: "Scheduled",
                ScheduledFor: futureDate,
                AuthorId: author.Id
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Scheduled", result.Status);

            var persisted = await context.Posts.FirstOrDefaultAsync(p => p.Id == result.Id);
            Assert.NotNull(persisted);
            Assert.Equal(futureDate, persisted.ScheduledFor);
        }
    }

    [Fact]
    public async Task CreateNewsPost_ScheduledPost_WithPastDate_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Kyle", LastName = "Reese", Email = "kyle@rebel.org" };
            context.Employees.Add(author);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var postRepo = new GenericRepository<Post>(context);
            var empRepo = new GenericRepository<Employee>(context);
            var validator = new CreateNewsPostCommandValidator();

            var handler = new CreateNewsPostCommandHandler(postRepo, empRepo, uow, validator);
            var pastDate = DateTime.UtcNow.AddHours(-2);

            var command = new CreateNewsPostCommand(
                Title: "Invalid Schedule",
                Content: "This has a past release date.",
                Category: "General",
                Status: "Scheduled",
                ScheduledFor: pastDate,
                AuthorId: author.Id
            );

            await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        }
    }

    [Fact]
    public async Task CreateNewsPost_OversizedMediaFile_ThrowsValidationException()
    {
        var validator = new CreateNewsPostCommandValidator();

        var formFile = new FormFile(
            baseStream: new MemoryStream(new byte[11 * 1024 * 1024]), // 11 MB
            baseStreamOffset: 0,
            length: 11 * 1024 * 1024,
            name: "mediaFile",
            fileName: "large_video.mp4"
        )
        {
            Headers = new HeaderDictionary(),
            ContentType = "video/mp4"
        };

        var command = new CreateNewsPostCommand(
            Title: "Big Video",
            Content: "Check this video out.",
            MediaFile: formFile
        );

        var result = await validator.ValidateAsync(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("10 MB limit"));
    }

    [Fact]
    public async Task NewsController_CreateNewsPost_ReturnsCreatedResult()
    {
        var fakeResponse = new NewsPostResponseDto(10, "Title", "Draft", "Created successfully");
        var fakeMediator = new FakeCreateMediator(fakeResponse);

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

        var model = new CreateNewsPostModel
        {
            Title = "Title",
            Content = "Content",
            Category = "General",
            Status = "Draft"
        };

        var actionResult = await controller.CreateNewsPost(model, null, CancellationToken.None);
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var response = Assert.IsType<NewsPostResponseDto>(objectResult.Value);
        Assert.Equal(10, response.Id);
    }

    private class FakeCreateMediator : ISender
    {
        private readonly object _result;

        public FakeCreateMediator(object result)
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
