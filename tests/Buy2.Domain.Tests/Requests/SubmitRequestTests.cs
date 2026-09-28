using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.Requests.SubmitRequest;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Buy2.Domain.Tests.Requests;

public class SubmitRequestTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task SubmitRequest_Success_ResolvesManagerAndInitializesPendingState()
    {
        var dbName = Guid.NewGuid().ToString();
        int empId, typeId, managerId;

        using (var context = CreateDbContext(dbName))
        {
            var manager = new Employee { FirstName = "Sarah", LastName = "Connor", Email = "manager@example.com" };
            context.Employees.Add(manager);
            await context.SaveChangesAsync();
            managerId = manager.Id;

            var emp = new Employee { FirstName = "John", LastName = "Connor", Email = "john@example.com", DirectManagerId = managerId };
            var reqType = new RequestType
            {
                Name = "Annual Leave",
                Category = "Leave",
                IsActive = true,
                RequiresDates = true,
                RequiresReason = false
            };
            context.Employees.Add(emp);
            context.RequestTypes.Add(reqType);
            await context.SaveChangesAsync();

            empId = emp.Id;
            typeId = reqType.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new SubmitRequestCommandHandler(
                new GenericRepository<Request>(context),
                new GenericRepository<Employee>(context),
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var command = new SubmitRequestCommand(
                EmployeeId: empId,
                RequestTypeId: typeId,
                StartDate: DateTime.UtcNow.AddDays(1),
                EndDate: DateTime.UtcNow.AddDays(5),
                Reason: "Summer holiday",
                CategoryValuesJson: "{\"type\":\"full\"}"
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("Pending", result.Status);
            Assert.Equal("Pending", result.ManagerStatus);
            Assert.Equal("Pending", result.HrStatus);
            Assert.Equal(managerId, result.ManagerId);
            Assert.Equal("Sarah Connor", result.ManagerName);
            Assert.Equal("Annual Leave", result.RequestTypeName);
            Assert.Equal("Leave", result.Category);

            var entity = await context.Requests.Include(r => r.Attachments).FirstOrDefaultAsync(r => r.Id == result.Id);
            Assert.NotNull(entity);
            Assert.Equal("Pending", entity.Status);
            Assert.Equal("Pending", entity.ManagerStatus);
            Assert.Equal("Pending", entity.HrStatus);
            Assert.Equal(managerId, entity.ManagerId);
        }
    }

    [Fact]
    public async Task SubmitRequest_WithAttachments_PersistsAttachmentMetadata()
    {
        var dbName = Guid.NewGuid().ToString();
        int empId, typeId;

        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
            var reqType = new RequestType { Name = "Medical Claim", Category = "Expense", IsActive = true, RequiresDates = false, RequiresReason = false };
            context.Employees.Add(emp);
            context.RequestTypes.Add(reqType);
            await context.SaveChangesAsync();

            empId = emp.Id;
            typeId = reqType.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new SubmitRequestCommandHandler(
                new GenericRepository<Request>(context),
                new GenericRepository<Employee>(context),
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var fileBytes = Encoding.UTF8.GetBytes("receipt content");
            var formFile = new FormFile(new MemoryStream(fileBytes), 0, fileBytes.Length, "receipt", "receipt.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var command = new SubmitRequestCommand(
                EmployeeId: empId,
                RequestTypeId: typeId,
                StartDate: null,
                EndDate: null,
                Reason: "Hospital visit",
                CategoryValuesJson: null,
                Attachments: new List<IFormFile> { formFile }
            );

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Single(result.Attachments);
            Assert.Equal("receipt.pdf", result.Attachments[0].FileName);
            Assert.Equal("application/pdf", result.Attachments[0].ContentType);
            Assert.Equal(fileBytes.Length, result.Attachments[0].FileSize);
        }
    }

    [Fact]
    public async Task SubmitRequest_MissingRequiredDates_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();
        int empId, typeId;

        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "Bob", LastName = "Brown", Email = "bob@example.com" };
            var reqType = new RequestType { Name = "Sick Leave", Category = "Leave", IsActive = true, RequiresDates = true, RequiresReason = false };
            context.Employees.Add(emp);
            context.RequestTypes.Add(reqType);
            await context.SaveChangesAsync();

            empId = emp.Id;
            typeId = reqType.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new SubmitRequestCommandHandler(
                new GenericRepository<Request>(context),
                new GenericRepository<Employee>(context),
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var command = new SubmitRequestCommand(
                EmployeeId: empId,
                RequestTypeId: typeId,
                StartDate: null,
                EndDate: null,
                Reason: "Sick",
                CategoryValuesJson: null
            );

            await Assert.ThrowsAsync<ValidationException>(() =>
                handler.Handle(command, CancellationToken.None));
        }
    }

    [Fact]
    public async Task SubmitRequest_MissingRequiredReason_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();
        int empId, typeId;

        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "Bob", LastName = "Brown", Email = "bob@example.com" };
            var reqType = new RequestType { Name = "Business Trip", Category = "General", IsActive = true, RequiresDates = false, RequiresReason = true };
            context.Employees.Add(emp);
            context.RequestTypes.Add(reqType);
            await context.SaveChangesAsync();

            empId = emp.Id;
            typeId = reqType.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new SubmitRequestCommandHandler(
                new GenericRepository<Request>(context),
                new GenericRepository<Employee>(context),
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var command = new SubmitRequestCommand(
                EmployeeId: empId,
                RequestTypeId: typeId,
                StartDate: null,
                EndDate: null,
                Reason: "",
                CategoryValuesJson: null
            );

            await Assert.ThrowsAsync<ValidationException>(() =>
                handler.Handle(command, CancellationToken.None));
        }
    }

    [Fact]
    public async Task SubmitRequest_InactiveRequestType_ThrowsInvalidOperationException()
    {
        var dbName = Guid.NewGuid().ToString();
        int empId, typeId;

        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "Charlie", LastName = "Chaplin", Email = "charlie@example.com" };
            var reqType = new RequestType { Name = "Old Request", Category = "General", IsActive = false };
            context.Employees.Add(emp);
            context.RequestTypes.Add(reqType);
            await context.SaveChangesAsync();

            empId = emp.Id;
            typeId = reqType.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new SubmitRequestCommandHandler(
                new GenericRepository<Request>(context),
                new GenericRepository<Employee>(context),
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var command = new SubmitRequestCommand(
                EmployeeId: empId,
                RequestTypeId: typeId,
                StartDate: null,
                EndDate: null,
                Reason: "Test",
                CategoryValuesJson: null
            );

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(command, CancellationToken.None));
        }
    }

    [Fact]
    public void SubmitRequestCommandValidator_ValidCommand_Passes()
    {
        var validator = new SubmitRequestCommandValidator();
        var command = new SubmitRequestCommand(
            EmployeeId: 1,
            RequestTypeId: 2,
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddDays(2),
            Reason: "Valid reason",
            CategoryValuesJson: "{}"
        );

        var result = validator.Validate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void SubmitRequestCommandValidator_EndDateBeforeStartDate_Fails()
    {
        var validator = new SubmitRequestCommandValidator();
        var command = new SubmitRequestCommand(
            EmployeeId: 1,
            RequestTypeId: 2,
            StartDate: DateTime.UtcNow.AddDays(5),
            EndDate: DateTime.UtcNow.AddDays(1),
            Reason: null,
            CategoryValuesJson: null
        );

        var result = validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "EndDate");
    }
}
