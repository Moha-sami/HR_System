using System.ComponentModel.DataAnnotations;
using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Employees;
using Buy2.Application.Features.Employees.DeleteDocument;
using Buy2.Application.Features.Employees.GetDocuments;
using Buy2.Application.Features.Employees.UploadDocument;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Employees;

public class EmployeeDocumentsTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetEmployeeDocuments_ReturnsOnlyDocumentsForGivenEmployee()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var doc1 = new EmployeeDocument
        {
            EmployeeId = 1,
            Category = "Contracts",
            StorageUrl = "https://storage.buy2.com/docs/contract1.pdf",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        var doc2 = new EmployeeDocument
        {
            EmployeeId = 1,
            Category = "Certificates",
            StorageUrl = "https://storage.buy2.com/docs/cert1.png",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var otherEmpDoc = new EmployeeDocument
        {
            EmployeeId = 2,
            Category = "Medical",
            StorageUrl = "https://storage.buy2.com/docs/med.pdf",
            CreatedAt = DateTime.UtcNow
        };

        context.EmployeeDocuments.AddRange(doc1, doc2, otherEmpDoc);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<EmployeeDocument>(context);
        var handler = new GetEmployeeDocumentsQueryHandler(repo);

        var result = await handler.Handle(new GetEmployeeDocumentsQuery(1), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, d => Assert.Equal(1, d.EmployeeId));
        // Verify order: newest first
        Assert.Equal(doc2.Id, result[0].Id);
        Assert.Equal("Certificates", result[0].Category);
        Assert.Equal("https://storage.buy2.com/docs/cert1.png", result[0].StorageUrl);
    }

    [Fact]
    public async Task GetEmployeeDocuments_ReturnsEmptyList_WhenNoDocumentsExist()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var repo = new GenericRepository<EmployeeDocument>(context);
        var handler = new GetEmployeeDocumentsQueryHandler(repo);

        var result = await handler.Handle(new GetEmployeeDocumentsQuery(999), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task DeleteEmployeeDocument_DeletesExistingDocumentAndReturnsTrue()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var doc = new EmployeeDocument
        {
            EmployeeId = 10,
            Category = "Identification",
            StorageUrl = "https://storage.buy2.com/docs/national_id.pdf"
        };
        context.EmployeeDocuments.Add(doc);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<EmployeeDocument>(context);
        var uow = new UnitOfWork(context);
        var handler = new DeleteEmployeeDocumentCommandHandler(repo, uow);

        var result = await handler.Handle(new DeleteEmployeeDocumentCommand(10, doc.Id), CancellationToken.None);

        Assert.True(result);
        var inDb = await context.EmployeeDocuments.FirstOrDefaultAsync(d => d.Id == doc.Id);
        Assert.Null(inDb);
    }

    [Fact]
    public async Task DeleteEmployeeDocument_ReturnsFalse_WhenDocumentNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var repo = new GenericRepository<EmployeeDocument>(context);
        var uow = new UnitOfWork(context);
        var handler = new DeleteEmployeeDocumentCommandHandler(repo, uow);

        var result = await handler.Handle(new DeleteEmployeeDocumentCommand(10, 999), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteEmployeeDocument_ReturnsFalse_WhenDocumentBelongsToDifferentEmployee()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var doc = new EmployeeDocument
        {
            EmployeeId = 20,
            Category = "Identification",
            StorageUrl = "https://storage.buy2.com/docs/passport.pdf"
        };
        context.EmployeeDocuments.Add(doc);
        await context.SaveChangesAsync();

        var repo = new GenericRepository<EmployeeDocument>(context);
        var uow = new UnitOfWork(context);
        var handler = new DeleteEmployeeDocumentCommandHandler(repo, uow);

        // Attempt to delete with employeeId 10 instead of 20
        var result = await handler.Handle(new DeleteEmployeeDocumentCommand(10, doc.Id), CancellationToken.None);

        Assert.False(result);
        var stillInDb = await context.EmployeeDocuments.FirstOrDefaultAsync(d => d.Id == doc.Id);
        Assert.NotNull(stillInDb);
    }

    [Fact]
    public async Task UploadEmployeeDocument_SavesDocument_WhenEmployeeExists()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var emp = new Employee
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@buy2.com"
        };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var empRepo = new GenericRepository<Employee>(context);
        var docRepo = new GenericRepository<EmployeeDocument>(context);
        var uow = new UnitOfWork(context);
        var handler = new UploadEmployeeDocumentCommandHandler(empRepo, docRepo, uow);

        var command = new UploadEmployeeDocumentCommand(emp.Id, "Other", "https://storage.buy2.com/docs/test.pdf");
        var docId = await handler.Handle(command, CancellationToken.None);

        Assert.True(docId > 0);
        var docInDb = await context.EmployeeDocuments.FirstOrDefaultAsync(d => d.Id == docId);
        Assert.NotNull(docInDb);
        Assert.Equal(emp.Id, docInDb.EmployeeId);
        Assert.Equal("Other", docInDb.Category);
        Assert.Equal("https://storage.buy2.com/docs/test.pdf", docInDb.StorageUrl);
    }

    [Fact]
    public async Task UploadEmployeeDocument_ThrowsValidationException_WhenEmployeeDoesNotExist()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateDbContext(dbName);

        var empRepo = new GenericRepository<Employee>(context);
        var docRepo = new GenericRepository<EmployeeDocument>(context);
        var uow = new UnitOfWork(context);
        var handler = new UploadEmployeeDocumentCommandHandler(empRepo, docRepo, uow);

        var command = new UploadEmployeeDocumentCommand(999, "Other", "https://storage.buy2.com/docs/test.pdf");
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }
}
