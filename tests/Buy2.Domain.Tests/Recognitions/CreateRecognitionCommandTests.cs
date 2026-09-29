using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Recognitions;
using Buy2.Application.Features.Recognitions.Commands.CreateRecognition;
using Buy2.Application.Features.Recognitions.Common;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using FluentValidation;
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

namespace Buy2.Domain.Tests.Recognitions;

public class CreateRecognitionCommandTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task CreateRecognition_PublishedWithPoints_PersistsRecognitionAndDispatchesPointsTransaction()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Manager", LastName = "User", Email = "mgr@corp.com" };
            var recipient = new Employee { FirstName = "Star", LastName = "Performer", Email = "star@corp.com", IsActive = true };
            context.Employees.AddRange(author, recipient);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var recogRepo = new GenericRepository<Recognition>(context);
            var empRepo = new GenericRepository<Employee>(context);
            var ptsRepo = new GenericRepository<PointsTransaction>(context);

            var handler = new CreateRecognitionHandler(recogRepo, empRepo, ptsRepo, uow);

            var command = new CreateRecognitionCommand(
                RecipientId: recipient.Id,
                Title: "Great Innovation",
                Narrative: "Shipped the new dashboard ahead of time.",
                AwardedPoints: 350,
                Badge: RecognitionLifecycleManager.BadgeInnovator,
                Status: RecognitionLifecycleManager.StatusPublished,
                AuthorId: author.Id
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("Great Innovation", result.Title);
            Assert.Equal(350, result.AwardedPoints);
            Assert.Equal("Published", result.Status);
        }

        using (var context = CreateDbContext(dbName))
        {
            var savedRecog = await context.Recognitions.FirstAsync();
            Assert.Equal("Great Innovation", savedRecog.Title);
            Assert.Equal(350, savedRecog.AwardedPoints);
            Assert.NotNull(savedRecog.PublishedAt);

            var savedPts = await context.PointsTransactions.FirstOrDefaultAsync();
            Assert.NotNull(savedPts);
            Assert.Equal(350, savedPts.Amount);
            Assert.Equal(TransactionType.Add, savedPts.TransactionType);
            Assert.Equal(RecognitionPointsGrantEngine.TriggeredByRecognition, savedPts.TriggeredBy);
        }
    }

    [Fact]
    public async Task CreateRecognition_DraftStatus_DoesNotDispatchPointsTransactionImmediately()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Manager", LastName = "User", Email = "mgr@corp.com" };
            var recipient = new Employee { FirstName = "Star", LastName = "Performer", Email = "star@corp.com", IsActive = true };
            context.Employees.AddRange(author, recipient);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var recogRepo = new GenericRepository<Recognition>(context);
            var empRepo = new GenericRepository<Employee>(context);
            var ptsRepo = new GenericRepository<PointsTransaction>(context);

            var handler = new CreateRecognitionHandler(recogRepo, empRepo, ptsRepo, uow);

            var command = new CreateRecognitionCommand(
                RecipientId: recipient.Id,
                Title: "Draft Kudos",
                Narrative: "Drafting words for review.",
                AwardedPoints: 200,
                Status: RecognitionLifecycleManager.StatusDraft,
                AuthorId: author.Id
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Draft", result.Status);
        }

        using (var context = CreateDbContext(dbName))
        {
            var savedRecog = await context.Recognitions.FirstAsync();
            Assert.Equal("Draft", savedRecog.Status);
            Assert.Null(savedRecog.PublishedAt);

            var pointsCount = await context.PointsTransactions.CountAsync();
            Assert.Equal(0, pointsCount);
        }
    }

    [Fact]
    public async Task CreateRecognition_ScheduledStatus_DoesNotDispatchPointsTransactionImmediately()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Manager", LastName = "User", Email = "mgr@corp.com" };
            var recipient = new Employee { FirstName = "Star", LastName = "Performer", Email = "star@corp.com", IsActive = true };
            context.Employees.AddRange(author, recipient);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var recogRepo = new GenericRepository<Recognition>(context);
            var empRepo = new GenericRepository<Employee>(context);
            var ptsRepo = new GenericRepository<PointsTransaction>(context);

            var handler = new CreateRecognitionHandler(recogRepo, empRepo, ptsRepo, uow);

            var futureDate = DateTime.UtcNow.AddDays(3);
            var command = new CreateRecognitionCommand(
                RecipientId: recipient.Id,
                Title: "Anniversary Celebration",
                Narrative: "Happy work anniversary!",
                AwardedPoints: 500,
                Status: RecognitionLifecycleManager.StatusScheduled,
                ScheduledFor: futureDate,
                AuthorId: author.Id
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Scheduled", result.Status);
        }

        using (var context = CreateDbContext(dbName))
        {
            var savedRecog = await context.Recognitions.FirstAsync();
            Assert.Equal("Scheduled", savedRecog.Status);
            Assert.NotNull(savedRecog.ScheduledFor);

            var pointsCount = await context.PointsTransactions.CountAsync();
            Assert.Equal(0, pointsCount);
        }
    }

    [Fact]
    public async Task CreateRecognition_NonExistentRecipient_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var author = new Employee { FirstName = "Manager", LastName = "User", Email = "mgr@corp.com" };
        context.Employees.Add(author);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var recogRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var ptsRepo = new GenericRepository<PointsTransaction>(context);

        var handler = new CreateRecognitionHandler(recogRepo, empRepo, ptsRepo, uow);

        var command = new CreateRecognitionCommand(
            RecipientId: 9999,
            Title: "Title",
            Narrative: "Body",
            AuthorId: author.Id
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRecognition_InactiveRecipient_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var author = new Employee { FirstName = "Manager", LastName = "User", Email = "mgr@corp.com" };
        var recipient = new Employee { FirstName = "Inactive", LastName = "User", Email = "inactive@corp.com", IsActive = false };
        context.Employees.AddRange(author, recipient);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var recogRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var ptsRepo = new GenericRepository<PointsTransaction>(context);

        var handler = new CreateRecognitionHandler(recogRepo, empRepo, ptsRepo, uow);

        var command = new CreateRecognitionCommand(
            RecipientId: recipient.Id,
            Title: "Title",
            Narrative: "Body",
            AuthorId: author.Id
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRecognition_SelfRecognition_ThrowsInvalidOperationException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var user = new Employee { FirstName = "Self", LastName = "User", Email = "self@corp.com", IsActive = true };
        context.Employees.Add(user);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var recogRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var ptsRepo = new GenericRepository<PointsTransaction>(context);

        var handler = new CreateRecognitionHandler(recogRepo, empRepo, ptsRepo, uow);

        var command = new CreateRecognitionCommand(
            RecipientId: user.Id,
            Title: "Self Award",
            Narrative: "I did great",
            AwardedPoints: 100,
            AuthorId: user.Id
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("themselves", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("", "Valid Narrative")]
    [InlineData("   ", "Valid Narrative")]
    [InlineData(null, "Valid Narrative")]
    [InlineData("Valid Title", "")]
    [InlineData("Valid Title", "   ")]
    [InlineData("Valid Title", null)]
    public async Task CreateRecognition_MissingRequiredFields_ThrowsValidationException(string? title, string? narrative)
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var recogRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var ptsRepo = new GenericRepository<PointsTransaction>(context);

        var handler = new CreateRecognitionHandler(recogRepo, empRepo, ptsRepo, uow);

        var command = new CreateRecognitionCommand(
            RecipientId: 1,
            Title: title!,
            Narrative: narrative!
        );

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRecognition_NegativePoints_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var recogRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var ptsRepo = new GenericRepository<PointsTransaction>(context);

        var handler = new CreateRecognitionHandler(recogRepo, empRepo, ptsRepo, uow);

        var command = new CreateRecognitionCommand(
            RecipientId: 1,
            Title: "Title",
            Narrative: "Body",
            AwardedPoints: -10
        );

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRecognition_OversizedPoints_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var recogRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var ptsRepo = new GenericRepository<PointsTransaction>(context);

        var handler = new CreateRecognitionHandler(recogRepo, empRepo, ptsRepo, uow);

        var command = new CreateRecognitionCommand(
            RecipientId: 1,
            Title: "Title",
            Narrative: "Body",
            AwardedPoints: 5001
        );

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRecognition_ScheduledWithoutFutureDate_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();

        using var context = CreateDbContext(dbName);
        var uow = new UnitOfWork(context);
        var recogRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var ptsRepo = new GenericRepository<PointsTransaction>(context);

        var handler = new CreateRecognitionHandler(recogRepo, empRepo, ptsRepo, uow);

        var command = new CreateRecognitionCommand(
            RecipientId: 1,
            Title: "Title",
            Narrative: "Body",
            Status: "Scheduled",
            ScheduledFor: null
        );

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRecognition_ControllerEndpoint_Returns201Created()
    {
        var expectedResponse = new RecognitionResponseDto(
            Id: 99,
            Title: "Team Hero",
            Status: "Published",
            AwardedPoints: 300,
            Message: "Recognition created successfully with status 'Published'."
        );

        var mediator = new FakeRecognitionsMediator(expectedResponse);
        var controller = new RecognitionsController(mediator);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "10"),
            new Claim(ClaimTypes.Role, "HR")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var model = new CreateRecognitionModel
        {
            RecipientId = 5,
            Title = "Team Hero",
            Narrative = "Helped the entire division.",
            AwardedPoints = 300,
            Badge = "TeamPlayer"
        };

        var actionResult = await controller.CreateRecognition(model, null, CancellationToken.None);

        var objResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objResult.StatusCode);
        var returnedDto = Assert.IsType<RecognitionResponseDto>(objResult.Value);
        Assert.Equal(99, returnedDto.Id);
        Assert.Equal("Team Hero", returnedDto.Title);
    }

    private class FakeRecognitionsMediator : ISender
    {
        private readonly object? _result;
        private readonly Exception? _exceptionToThrow;

        public FakeRecognitionsMediator(object? result = null, Exception? exceptionToThrow = null)
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
