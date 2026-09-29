using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Recognitions;
using Buy2.Application.Features.Recognitions.Commands.UpdateRecognition;
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

public class UpdateRecognitionCommandTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task UpdateRecognition_NonExistent_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);
        var recRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var ptRepo = new GenericRepository<PointsTransaction>(context);
        var uow = new UnitOfWork(context);

        var handler = new UpdateRecognitionHandler(recRepo, empRepo, ptRepo, uow);
        var command = new UpdateRecognitionCommand(
            Id: 999,
            RecipientId: 1,
            Title: "Updated Title",
            Narrative: "Updated Narrative",
            AwardedPoints: 100,
            ModifyingUserId: 1,
            IsElevatedUser: true
        );

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateRecognition_UnauthorizedCaller_ThrowsUnauthorizedAccessException()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var author = new Employee { FirstName = "Alice", LastName = "Author", Email = "author@corp.com" };
        var recipient = new Employee { FirstName = "Bob", LastName = "Recipient", Email = "rec@corp.com" };
        context.Employees.AddRange(author, recipient);
        await context.SaveChangesAsync();

        var recognition = new Recognition
        {
            AuthorId = author.Id,
            RecipientId = recipient.Id,
            Title = "Original Title",
            Narrative = "Original Narrative",
            AwardedPoints = 100,
            Status = RecognitionLifecycleManager.StatusPublished
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var recRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var ptRepo = new GenericRepository<PointsTransaction>(context);
        var uow = new UnitOfWork(context);

        var handler = new UpdateRecognitionHandler(recRepo, empRepo, ptRepo, uow);
        var command = new UpdateRecognitionCommand(
            Id: recognition.Id,
            RecipientId: recipient.Id,
            Title: "Hacked Title",
            Narrative: "Hacked Narrative",
            AwardedPoints: 200,
            ModifyingUserId: 9999, // not author, not elevated
            IsElevatedUser: false
        );

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateRecognition_SelfRecognition_ThrowsInvalidOperationException()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var author = new Employee { FirstName = "Alice", LastName = "Author", Email = "author@corp.com" };
        var recipient = new Employee { FirstName = "Bob", LastName = "Recipient", Email = "rec@corp.com" };
        context.Employees.AddRange(author, recipient);
        await context.SaveChangesAsync();

        var recognition = new Recognition
        {
            AuthorId = author.Id,
            RecipientId = recipient.Id,
            Title = "Original Title",
            Narrative = "Original Narrative",
            AwardedPoints = 100,
            Status = RecognitionLifecycleManager.StatusPublished
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var recRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var ptRepo = new GenericRepository<PointsTransaction>(context);
        var uow = new UnitOfWork(context);

        var handler = new UpdateRecognitionHandler(recRepo, empRepo, ptRepo, uow);
        var command = new UpdateRecognitionCommand(
            Id: recognition.Id,
            RecipientId: author.Id, // author recognizing self!
            Title: "Self Award",
            Narrative: "I am great",
            AwardedPoints: 50,
            ModifyingUserId: author.Id,
            IsElevatedUser: false
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateRecognition_PointsUpwardAdjustment_CreatesAddPointsTransaction()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var author = new Employee { FirstName = "Manager", LastName = "One", Email = "mgr@corp.com" };
        var recipient = new Employee { FirstName = "Dev", LastName = "Lead", Email = "dev@corp.com" };
        context.Employees.AddRange(author, recipient);
        await context.SaveChangesAsync();

        var recognition = new Recognition
        {
            AuthorId = author.Id,
            RecipientId = recipient.Id,
            Title = "Good Sprint",
            Narrative = "Delivered all tickets",
            AwardedPoints = 200,
            Status = RecognitionLifecycleManager.StatusPublished,
            PublishedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var recRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var ptRepo = new GenericRepository<PointsTransaction>(context);
        var uow = new UnitOfWork(context);

        var handler = new UpdateRecognitionHandler(recRepo, empRepo, ptRepo, uow);
        var command = new UpdateRecognitionCommand(
            Id: recognition.Id,
            RecipientId: recipient.Id,
            Title: "Great Sprint (Upgraded)",
            Narrative: "Delivered all tickets plus tech debt",
            AwardedPoints: 350, // +150 delta
            Status: "Published",
            ModifyingUserId: author.Id,
            IsElevatedUser: false
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Great Sprint (Upgraded)", result.Title);
        Assert.Equal(350, result.AwardedPoints);

        var transactions = await context.PointsTransactions.ToListAsync();
        Assert.Single(transactions);
        var tx = transactions.First();
        Assert.Equal(recipient.Id, tx.EmployeeId);
        Assert.Equal(150, tx.Amount);
        Assert.Equal(TransactionType.Add, tx.TransactionType);
        Assert.Equal(RecognitionPointsGrantEngine.TriggeredByRecognition, tx.TriggeredBy);
    }

    [Fact]
    public async Task UpdateRecognition_PointsDownwardAdjustment_CreatesDeductPointsTransaction()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var author = new Employee { FirstName = "HR", LastName = "Admin", Email = "hr@corp.com" };
        var recipient = new Employee { FirstName = "Employee", LastName = "Two", Email = "emp2@corp.com" };
        context.Employees.AddRange(author, recipient);
        await context.SaveChangesAsync();

        var recognition = new Recognition
        {
            AuthorId = author.Id,
            RecipientId = recipient.Id,
            Title = "Annual Recognition",
            Narrative = "Great job",
            AwardedPoints = 1000,
            Status = RecognitionLifecycleManager.StatusPublished
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var recRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var ptRepo = new GenericRepository<PointsTransaction>(context);
        var uow = new UnitOfWork(context);

        var handler = new UpdateRecognitionHandler(recRepo, empRepo, ptRepo, uow);
        var command = new UpdateRecognitionCommand(
            Id: recognition.Id,
            RecipientId: recipient.Id,
            Title: "Annual Recognition (Corrected)",
            Narrative: "Great job",
            AwardedPoints: 800, // -200 delta
            Status: "Published",
            ModifyingUserId: 555,
            IsElevatedUser: true
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(800, result.AwardedPoints);

        var transactions = await context.PointsTransactions.ToListAsync();
        Assert.Single(transactions);
        var tx = transactions.First();
        Assert.Equal(recipient.Id, tx.EmployeeId);
        Assert.Equal(200, tx.Amount);
        Assert.Equal(TransactionType.Deduct, tx.TransactionType);
        Assert.Equal(RecognitionPointsGrantEngine.TriggeredByRollback, tx.TriggeredBy);
    }

    [Fact]
    public async Task UpdateRecognition_RecipientChanged_RollsBackOldAndGrantsNew()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var author = new Employee { FirstName = "Team", LastName = "Lead", Email = "lead@corp.com" };
        var oldRecipient = new Employee { FirstName = "Wrong", LastName = "Person", Email = "wrong@corp.com" };
        var newRecipient = new Employee { FirstName = "Right", LastName = "Person", Email = "right@corp.com" };
        context.Employees.AddRange(author, oldRecipient, newRecipient);
        await context.SaveChangesAsync();

        var recognition = new Recognition
        {
            AuthorId = author.Id,
            RecipientId = oldRecipient.Id,
            Title = "Bug Smasher",
            Narrative = "Fixed prod bug",
            AwardedPoints = 300,
            Status = RecognitionLifecycleManager.StatusPublished
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var recRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var ptRepo = new GenericRepository<PointsTransaction>(context);
        var uow = new UnitOfWork(context);

        var handler = new UpdateRecognitionHandler(recRepo, empRepo, ptRepo, uow);
        var command = new UpdateRecognitionCommand(
            Id: recognition.Id,
            RecipientId: newRecipient.Id,
            Title: "Bug Smasher",
            Narrative: "Fixed prod bug",
            AwardedPoints: 300,
            Status: "Published",
            ModifyingUserId: author.Id,
            IsElevatedUser: false
        );

        var result = await handler.Handle(command, CancellationToken.None);

        var updatedRecognition = await context.Recognitions.FindAsync(recognition.Id);
        Assert.Equal(newRecipient.Id, updatedRecognition!.RecipientId);

        var transactions = await context.PointsTransactions.OrderBy(t => t.Id).ToListAsync();
        Assert.Equal(2, transactions.Count);

        var rollbackTx = transactions.First(t => t.TransactionType == TransactionType.Deduct);
        Assert.Equal(oldRecipient.Id, rollbackTx.EmployeeId);
        Assert.Equal(300, rollbackTx.Amount);

        var grantTx = transactions.First(t => t.TransactionType == TransactionType.Add);
        Assert.Equal(newRecipient.Id, grantTx.EmployeeId);
        Assert.Equal(300, grantTx.Amount);
    }

    [Fact]
    public async Task UpdateRecognition_PublishedToDraft_RollsBackPoints()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var author = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@corp.com" };
        var recipient = new Employee { FirstName = "Bob", LastName = "Jones", Email = "bob@corp.com" };
        context.Employees.AddRange(author, recipient);
        await context.SaveChangesAsync();

        var recognition = new Recognition
        {
            AuthorId = author.Id,
            RecipientId = recipient.Id,
            Title = "Accidental Publish",
            Narrative = "Premature post",
            AwardedPoints = 400,
            Status = RecognitionLifecycleManager.StatusPublished
        };
        context.Recognitions.Add(recognition);
        await context.SaveChangesAsync();

        var recRepo = new GenericRepository<Recognition>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var ptRepo = new GenericRepository<PointsTransaction>(context);
        var uow = new UnitOfWork(context);

        var handler = new UpdateRecognitionHandler(recRepo, empRepo, ptRepo, uow);
        var command = new UpdateRecognitionCommand(
            Id: recognition.Id,
            RecipientId: recipient.Id,
            Title: "Accidental Publish",
            Narrative: "Premature post",
            AwardedPoints: 400,
            Status: "Draft",
            ModifyingUserId: author.Id,
            IsElevatedUser: false
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Draft", result.Status);

        var transactions = await context.PointsTransactions.ToListAsync();
        Assert.Single(transactions);
        var tx = transactions.First();
        Assert.Equal(recipient.Id, tx.EmployeeId);
        Assert.Equal(400, tx.Amount);
        Assert.Equal(TransactionType.Deduct, tx.TransactionType);
    }

    [Fact]
    public async Task Controller_UpdateRecognition_ReturnsOk()
    {
        var responseDto = new RecognitionResponseDto(10, "Updated", "Published", 150, "Recognition updated successfully.");
        var fakeMediator = new FakeRecognitionsMediator(result: responseDto);
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

        var model = new UpdateRecognitionModel
        {
            RecipientId = 2,
            Title = "Updated",
            Narrative = "Updated narrative",
            AwardedPoints = 150,
            Status = "Published"
        };

        var actionResult = await controller.UpdateRecognition(10, model, null, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returned = Assert.IsType<RecognitionResponseDto>(okResult.Value);
        Assert.Equal("Updated", returned.Title);
        Assert.Equal(150, returned.AwardedPoints);
    }

    [Fact]
    public async Task Controller_UpdateRecognition_Forbidden_Returns403()
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

        var model = new UpdateRecognitionModel { RecipientId = 2, Title = "T", Narrative = "N" };
        var actionResult = await controller.UpdateRecognition(1, model, null, CancellationToken.None);
        var objResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, objResult.StatusCode);
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
