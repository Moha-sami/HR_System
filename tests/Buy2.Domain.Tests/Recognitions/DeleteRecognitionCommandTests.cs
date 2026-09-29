using Buy2.Api.Controllers;
using Buy2.Application.Features.Recognitions.Commands.DeleteRecognition;
using Buy2.Application.Features.Recognitions.Common;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
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

namespace Buy2.Domain.Tests.Recognitions;

public class DeleteRecognitionCommandTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task DeleteRecognition_NonExistent_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var recRepo = new GenericRepository<Recognition>(context);
        var ptRepo = new GenericRepository<PointsTransaction>(context);
        var uow = new UnitOfWork(context);

        var handler = new DeleteRecognitionHandler(recRepo, ptRepo, uow);
        var command = new DeleteRecognitionCommand(Id: 999, CurrentUserId: 1, IsElevatedUser: true);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRecognition_AlreadyDeleted_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var recognition = new Recognition
        {
            AuthorId = 1,
            RecipientId = 2,
            Title = "Deleted post",
            Narrative = "Already removed",
            IsDeleted = true,
            Status = RecognitionLifecycleManager.StatusPublished
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var recRepo = new GenericRepository<Recognition>(context);
        var ptRepo = new GenericRepository<PointsTransaction>(context);
        var uow = new UnitOfWork(context);

        var handler = new DeleteRecognitionHandler(recRepo, ptRepo, uow);
        var command = new DeleteRecognitionCommand(Id: recognition.Id, CurrentUserId: 1, IsElevatedUser: true);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRecognition_UnauthorizedCaller_ThrowsUnauthorizedAccessException()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var recognition = new Recognition
        {
            AuthorId = 10,
            RecipientId = 20,
            Title = "Important Post",
            Narrative = "Recognizing great work",
            AwardedPoints = 100,
            Status = RecognitionLifecycleManager.StatusPublished
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var recRepo = new GenericRepository<Recognition>(context);
        var ptRepo = new GenericRepository<PointsTransaction>(context);
        var uow = new UnitOfWork(context);

        var handler = new DeleteRecognitionHandler(recRepo, ptRepo, uow);
        var command = new DeleteRecognitionCommand(Id: recognition.Id, CurrentUserId: 99, IsElevatedUser: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRecognition_PublishedWithPoints_CreditsRollbackTransactionAndSoftDeletes()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var author = new Employee { FirstName = "Manager", LastName = "Boss", Email = "boss@corp.com" };
        var recipient = new Employee { FirstName = "Developer", LastName = "Star", Email = "star@corp.com" };
        context.Employees.AddRange(author, recipient);
        await context.SaveChangesAsync();

        var recognition = new Recognition
        {
            AuthorId = author.Id,
            RecipientId = recipient.Id,
            Title = "Release Hero",
            Narrative = "Helped deploy v2.0",
            AwardedPoints = 600,
            Status = RecognitionLifecycleManager.StatusPublished,
            PublishedAt = DateTime.UtcNow.AddDays(-2)
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var recRepo = new GenericRepository<Recognition>(context);
        var ptRepo = new GenericRepository<PointsTransaction>(context);
        var uow = new UnitOfWork(context);

        var handler = new DeleteRecognitionHandler(recRepo, ptRepo, uow);
        var command = new DeleteRecognitionCommand(Id: recognition.Id, CurrentUserId: 555, IsElevatedUser: true);

        var result = await handler.Handle(command, CancellationToken.None);
        Assert.True(result);

        // Verify soft-deleted
        var deletedRecognition = await context.Recognitions.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == recognition.Id);
        Assert.NotNull(deletedRecognition);
        Assert.True(deletedRecognition.IsDeleted);
        Assert.NotNull(deletedRecognition.DeletedAt);

        // Verify points rollback transaction staged
        var transactions = await context.PointsTransactions.ToListAsync();
        Assert.Single(transactions);
        var tx = transactions.First();
        Assert.Equal(recipient.Id, tx.EmployeeId);
        Assert.Equal(600, tx.Amount);
        Assert.Equal(TransactionType.Deduct, tx.TransactionType);
        Assert.Equal(RecognitionPointsGrantEngine.TriggeredByRollback, tx.TriggeredBy);
        Assert.Equal(555, tx.CreatedByUserId);
    }

    [Fact]
    public async Task DeleteRecognition_DraftPost_DoesNotCreateRollbackTransaction()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var author = new Employee { FirstName = "Alice", LastName = "Wonder", Email = "alice@corp.com" };
        var recipient = new Employee { FirstName = "Bob", LastName = "Ross", Email = "bob@corp.com" };
        context.Employees.AddRange(author, recipient);
        await context.SaveChangesAsync();

        var recognition = new Recognition
        {
            AuthorId = author.Id,
            RecipientId = recipient.Id,
            Title = "Draft Post",
            Narrative = "Still drafting",
            AwardedPoints = 300,
            Status = RecognitionLifecycleManager.StatusDraft
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var recRepo = new GenericRepository<Recognition>(context);
        var ptRepo = new GenericRepository<PointsTransaction>(context);
        var uow = new UnitOfWork(context);

        var handler = new DeleteRecognitionHandler(recRepo, ptRepo, uow);
        var command = new DeleteRecognitionCommand(Id: recognition.Id, CurrentUserId: author.Id, IsElevatedUser: false);

        var result = await handler.Handle(command, CancellationToken.None);
        Assert.True(result);

        var transactions = await context.PointsTransactions.ToListAsync();
        Assert.Empty(transactions);

        var deletedRecognition = await context.Recognitions.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == recognition.Id);
        Assert.NotNull(deletedRecognition);
        Assert.True(deletedRecognition.IsDeleted);
    }

    [Fact]
    public async Task Controller_DeleteRecognition_Returns204NoContent()
    {
        var fakeMediator = new FakeRecognitionsMediator(result: true);
        var controller = new RecognitionsController(fakeMediator);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var actionResult = await controller.DeleteRecognition(10, CancellationToken.None);
        Assert.IsType<NoContentResult>(actionResult);
    }

    [Fact]
    public async Task Controller_DeleteRecognition_Forbidden_Returns403()
    {
        var fakeMediator = new FakeRecognitionsMediator(exceptionToThrow: new UnauthorizedAccessException("Forbidden"));
        var controller = new RecognitionsController(fakeMediator);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "5")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var actionResult = await controller.DeleteRecognition(10, CancellationToken.None);
        var objResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, objResult.StatusCode);
    }

    [Fact]
    public async Task Controller_DeleteRecognition_NotFound_Returns404()
    {
        var fakeMediator = new FakeRecognitionsMediator(exceptionToThrow: new KeyNotFoundException("Not found"));
        var controller = new RecognitionsController(fakeMediator);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var actionResult = await controller.DeleteRecognition(999, CancellationToken.None);
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
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
