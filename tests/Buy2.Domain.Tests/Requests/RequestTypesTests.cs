using Buy2.Api.Controllers;
using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.Requests.CreateRequestType;
using Buy2.Application.Features.Requests.DeleteRequestType;
using Buy2.Application.Features.Requests.GetRequestTypeById;
using Buy2.Application.Features.Requests.GetRequestTypes;
using Buy2.Application.Features.Requests.UpdateRequestType;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Buy2.Domain.Tests.Requests;

public class RequestTypesTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    #region AC 1: Directory lists all request types with active status, categories, and policy configurations

    [Fact]
    public async Task GetRequestTypes_ReturnsAllRequestTypesOrdered()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            context.RequestTypes.AddRange(
                new RequestType { Name = "Sick Leave", Category = "Leave", IsActive = true, RequiresDates = true, RequiresReason = true },
                new RequestType { Name = "Annual Leave", Category = "Leave", IsActive = true, RequiresDates = true, RequiresReason = false },
                new RequestType { Name = "Equipment Requisition", Category = "Equipment", IsActive = true, RequiresDates = false, RequiresReason = true }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetRequestTypesQueryHandler(
                new GenericRepository<RequestType>(context),
                new GenericRepository<Request>(context));
            var result = (await handler.Handle(new GetRequestTypesQuery(), CancellationToken.None)).ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal("Equipment", result[0].Category);
            Assert.Equal("Annual Leave", result[1].Name);
            Assert.Equal("Sick Leave", result[2].Name);
        }
    }

    [Fact]
    public async Task GetRequestTypes_FiltersByCategory()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            context.RequestTypes.AddRange(
                new RequestType { Name = "Sick Leave", Category = "Leave", IsActive = true },
                new RequestType { Name = "Laptop Request", Category = "Equipment", IsActive = true }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetRequestTypesQueryHandler(
                new GenericRepository<RequestType>(context),
                new GenericRepository<Request>(context));
            var result = (await handler.Handle(new GetRequestTypesQuery(Category: "Equipment"), CancellationToken.None)).ToList();

            Assert.Single(result);
            Assert.Equal("Laptop Request", result[0].Name);
            Assert.Equal("Equipment", result[0].Category);
        }
    }

    [Fact]
    public async Task GetRequestTypes_FiltersByActiveStatus()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            context.RequestTypes.AddRange(
                new RequestType { Name = "Active Type", Category = "General", IsActive = true },
                new RequestType { Name = "Inactive Type", Category = "General", IsActive = false }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetRequestTypesQueryHandler(
                new GenericRepository<RequestType>(context),
                new GenericRepository<Request>(context));
            var activeOnly = (await handler.Handle(new GetRequestTypesQuery(IsActive: true), CancellationToken.None)).ToList();
            var inactiveOnly = (await handler.Handle(new GetRequestTypesQuery(IsActive: false), CancellationToken.None)).ToList();

            Assert.Single(activeOnly);
            Assert.Equal("Active Type", activeOnly[0].Name);

            Assert.Single(inactiveOnly);
            Assert.Equal("Inactive Type", inactiveOnly[0].Name);
        }
    }

    [Fact]
    public async Task GetRequestTypes_FiltersBySearch()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            context.RequestTypes.AddRange(
                new RequestType { Name = "Parental Leave", Category = "Leave", Hint = "For maternity or paternity", IsActive = true },
                new RequestType { Name = "Desk Monitor", Category = "Hardware", Hint = "Secondary display", IsActive = true }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetRequestTypesQueryHandler(
                new GenericRepository<RequestType>(context),
                new GenericRepository<Request>(context));

            var resultByName = (await handler.Handle(new GetRequestTypesQuery(Search: "Parental"), CancellationToken.None)).ToList();
            Assert.Single(resultByName);
            Assert.Equal("Parental Leave", resultByName[0].Name);

            var resultByHint = (await handler.Handle(new GetRequestTypesQuery(Search: "maternity"), CancellationToken.None)).ToList();
            Assert.Single(resultByHint);
            Assert.Equal("Parental Leave", resultByHint[0].Name);

            var resultByCategory = (await handler.Handle(new GetRequestTypesQuery(Search: "Hardware"), CancellationToken.None)).ToList();
            Assert.Single(resultByCategory);
            Assert.Equal("Desk Monitor", resultByCategory[0].Name);
        }
    }

    [Fact]
    public async Task GetRequestTypes_ComputesIsUtilizedFlag()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var rt1 = new RequestType { Name = "Used Type", Category = "Leave", IsActive = true };
            var rt2 = new RequestType { Name = "Unused Type", Category = "Leave", IsActive = true };
            context.RequestTypes.AddRange(rt1, rt2);
            await context.SaveChangesAsync();

            context.Requests.Add(new Request
            {
                EmployeeId = 1,
                RequestTypeId = rt1.Id,
                Status = "Pending"
            });
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetRequestTypesQueryHandler(
                new GenericRepository<RequestType>(context),
                new GenericRepository<Request>(context));

            var result = (await handler.Handle(new GetRequestTypesQuery(), CancellationToken.None)).ToList();
            var used = result.First(r => r.Name == "Used Type");
            var unused = result.First(r => r.Name == "Unused Type");

            Assert.True(used.IsUtilized);
            Assert.False(unused.IsUtilized);
        }
    }

    [Fact]
    public async Task GetRequestTypes_PaginatesResults()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            context.RequestTypes.AddRange(
                new RequestType { Name = "Type A", Category = "General", IsActive = true },
                new RequestType { Name = "Type B", Category = "General", IsActive = true },
                new RequestType { Name = "Type C", Category = "General", IsActive = true }
            );
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetRequestTypesQueryHandler(
                new GenericRepository<RequestType>(context),
                new GenericRepository<Request>(context));

            var page1 = (await handler.Handle(new GetRequestTypesQuery(PageNumber: 1, PageSize: 2), CancellationToken.None)).ToList();
            var page2 = (await handler.Handle(new GetRequestTypesQuery(PageNumber: 2, PageSize: 2), CancellationToken.None)).ToList();

            Assert.Equal(2, page1.Count);
            Assert.Single(page2);
        }
    }


    [Fact]
    public async Task GetRequestTypeById_ReturnsCorrectDto()
    {
        var dbName = Guid.NewGuid().ToString();
        int id;
        using (var context = CreateDbContext(dbName))
        {
            var rt = new RequestType
            {
                Name = "Study Leave",
                Category = "Leave",
                Hint = "For exams and coursework",
                LeaveType = "Full",
                LeavePay = "Paid",
                RequiresDates = true,
                RequiresReason = true,
                IsActive = true,
                AddedBy = "HR Admin"
            };
            context.RequestTypes.Add(rt);
            await context.SaveChangesAsync();
            id = rt.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetRequestTypeByIdQueryHandler(new GenericRepository<RequestType>(context));
            var result = await handler.Handle(new GetRequestTypeByIdQuery(id), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
            Assert.Equal("Study Leave", result.Name);
            Assert.Equal("Leave", result.Category);
            Assert.Equal("For exams and coursework", result.Hint);
            Assert.Equal("Full", result.LeaveType);
            Assert.Equal("Paid", result.LeavePay);
            Assert.True(result.RequiresDates);
            Assert.True(result.RequiresReason);
            Assert.True(result.IsActive);
            Assert.Equal("HR Admin", result.AddedBy);
        }
    }

    [Fact]
    public async Task GetRequestTypeById_NonExistent_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetRequestTypeByIdQueryHandler(new GenericRepository<RequestType>(context));
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                handler.Handle(new GetRequestTypeByIdQuery(999), CancellationToken.None));
        }
    }

    #endregion

    #region AC 2: Supports creating request types with conditional rules for leaves, reasons, and date bounds

    [Fact]
    public async Task CreateRequestType_Success_PersistsAllProperties()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var handler = new CreateRequestTypeCommandHandler(
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var dto = new CreateRequestTypeDto(
                Category: "Leave",
                Name: "Bereavement Leave",
                Hint: "For immediate family bereavement",
                LeaveType: "Full",
                LeavePay: "Paid",
                RequiresDates: true,
                RequiresReason: true,
                IsActive: true,
                AddedBy: "AdminUser"
            );

            var result = await handler.Handle(new CreateRequestTypeCommand(dto), CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("Bereavement Leave", result.Name);
            Assert.Equal("Leave", result.Category);
            Assert.Equal("For immediate family bereavement", result.Hint);
            Assert.Equal("Full", result.LeaveType);
            Assert.Equal("Paid", result.LeavePay);
            Assert.True(result.RequiresDates);
            Assert.True(result.RequiresReason);
            Assert.True(result.IsActive);
            Assert.Equal("AdminUser", result.AddedBy);
            Assert.NotEqual(default, result.CreatedAt);

            var entity = await context.RequestTypes.FindAsync(result.Id);
            Assert.NotNull(entity);
            Assert.Equal("Bereavement Leave", entity.Name);
        }
    }

    [Theory]
    [InlineData("", "Leave")]
    [InlineData("   ", "Leave")]
    [InlineData(null, "Leave")]
    public async Task CreateRequestType_EmptyName_ThrowsValidationException(string? name, string category)
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var handler = new CreateRequestTypeCommandHandler(
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var dto = new CreateRequestTypeDto(category, name!, null, null, null, false, false);
            await Assert.ThrowsAsync<ValidationException>(() =>
                handler.Handle(new CreateRequestTypeCommand(dto), CancellationToken.None));
        }
    }

    [Theory]
    [InlineData("Annual Leave", "")]
    [InlineData("Annual Leave", "   ")]
    [InlineData("Annual Leave", null)]
    public async Task CreateRequestType_EmptyCategory_ThrowsValidationException(string name, string? category)
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var handler = new CreateRequestTypeCommandHandler(
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var dto = new CreateRequestTypeDto(category!, name, null, null, null, false, false);
            await Assert.ThrowsAsync<ValidationException>(() =>
                handler.Handle(new CreateRequestTypeCommand(dto), CancellationToken.None));
        }
    }

    [Fact]
    public async Task CreateRequestType_DuplicateNameInCategory_ThrowsInvalidOperationException()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            context.RequestTypes.Add(new RequestType { Name = "Casual Leave", Category = "Leave" });
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new CreateRequestTypeCommandHandler(
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var dto = new CreateRequestTypeDto("Leave", "Casual Leave", null, "Full", "Paid", true, true);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new CreateRequestTypeCommand(dto), CancellationToken.None));
        }
    }

    [Fact]
    public async Task CreateRequestType_SameNameInDifferentCategory_Succeeds()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            context.RequestTypes.Add(new RequestType { Name = "Urgent", Category = "Leave" });
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new CreateRequestTypeCommandHandler(
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var dto = new CreateRequestTypeDto("Expense", "Urgent", null, null, null, false, true);
            var result = await handler.Handle(new CreateRequestTypeCommand(dto), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Urgent", result.Name);
            Assert.Equal("Expense", result.Category);
        }
    }

    [Fact]
    public async Task CreateRequestType_InvalidLeaveType_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var handler = new CreateRequestTypeCommandHandler(
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var dto = new CreateRequestTypeDto("Leave", "Special Leave", null, "QuarterDay", "Paid", true, true);
            await Assert.ThrowsAsync<ValidationException>(() =>
                handler.Handle(new CreateRequestTypeCommand(dto), CancellationToken.None));
        }
    }

    [Fact]
    public void CreateRequestTypeCommandValidator_ValidCommand_Passes()
    {
        var validator = new CreateRequestTypeCommandValidator();
        var cmd = new CreateRequestTypeCommand(new CreateRequestTypeDto(
            Category: "Leave",
            Name: "Maternity Leave",
            Hint: "Standard leave",
            LeaveType: "Full",
            LeavePay: "Paid",
            RequiresDates: true,
            RequiresReason: true
        ));

        var result = validator.Validate(cmd);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateRequestTypeCommandValidator_InvalidInputs_Fails()
    {
        var validator = new CreateRequestTypeCommandValidator();
        var cmd = new CreateRequestTypeCommand(new CreateRequestTypeDto(
            Category: "",
            Name: "",
            Hint: null,
            LeaveType: "Invalid",
            LeavePay: "Invalid",
            RequiresDates: false,
            RequiresReason: false
        ));

        var result = validator.Validate(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Name"));
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Category"));
    }

    [Fact]
    public async Task CreateRequestType_InvalidLeavePay_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var handler = new CreateRequestTypeCommandHandler(
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var dto = new CreateRequestTypeDto("Leave", "Special Leave", null, "Full", "HalfSalary", true, true);
            await Assert.ThrowsAsync<ValidationException>(() =>
                handler.Handle(new CreateRequestTypeCommand(dto), CancellationToken.None));
        }
    }

    [Fact]
    public async Task CreateRequestType_NonLeaveWithLeaveFields_ThrowsValidationException()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var handler = new CreateRequestTypeCommandHandler(
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var dto = new CreateRequestTypeDto("General", "Office Supplies", null, "Full", null, false, false);
            await Assert.ThrowsAsync<ValidationException>(() =>
                handler.Handle(new CreateRequestTypeCommand(dto), CancellationToken.None));
        }
    }

    #endregion

    #region AC 3: Provides update capabilities for existing request types while protecting data integrity

    [Fact]
    public async Task UpdateRequestType_Success_UpdatesAllFields()
    {
        var dbName = Guid.NewGuid().ToString();
        int id;
        using (var context = CreateDbContext(dbName))
        {
            var rt = new RequestType
            {
                Name = "Old Leave",
                Category = "Leave",
                Hint = "Old Hint",
                LeaveType = "Full",
                LeavePay = "Paid",
                RequiresDates = false,
                RequiresReason = false,
                IsActive = true
            };
            context.RequestTypes.Add(rt);
            await context.SaveChangesAsync();
            id = rt.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new UpdateRequestTypeCommandHandler(
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var dto = new UpdateRequestTypeDto(
                Category: "Leave",
                Name: "Updated Leave",
                Hint: "New Hint",
                LeaveType: "Partial",
                LeavePay: "Unpaid",
                RequiresDates: true,
                RequiresReason: true,
                IsActive: false
            );

            var result = await handler.Handle(new UpdateRequestTypeCommand(id, dto), CancellationToken.None);

            Assert.Equal("Updated Leave", result.Name);
            Assert.Equal("New Hint", result.Hint);
            Assert.Equal("Partial", result.LeaveType);
            Assert.Equal("Unpaid", result.LeavePay);
            Assert.True(result.RequiresDates);
            Assert.True(result.RequiresReason);
            Assert.False(result.IsActive);

            var entity = await context.RequestTypes.FindAsync(id);
            Assert.NotNull(entity);
            Assert.Equal("Updated Leave", entity.Name);
            Assert.False(entity.IsActive);
        }
    }

    [Fact]
    public async Task UpdateRequestType_NonExistent_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var handler = new UpdateRequestTypeCommandHandler(
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var dto = new UpdateRequestTypeDto("Leave", "Test", null, null, null, false, false, true);
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                handler.Handle(new UpdateRequestTypeCommand(999, dto), CancellationToken.None));
        }
    }

    [Fact]
    public async Task UpdateRequestType_DuplicateNameInCategory_ThrowsInvalidOperationException()
    {
        var dbName = Guid.NewGuid().ToString();
        int id1;
        using (var context = CreateDbContext(dbName))
        {
            var rt1 = new RequestType { Name = "Leave A", Category = "Leave" };
            var rt2 = new RequestType { Name = "Leave B", Category = "Leave" };
            context.RequestTypes.AddRange(rt1, rt2);
            await context.SaveChangesAsync();
            id1 = rt1.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new UpdateRequestTypeCommandHandler(
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            // Attempt to rename Leave A to Leave B
            var dto = new UpdateRequestTypeDto("Leave", "Leave B", null, "Full", "Paid", true, true, true);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new UpdateRequestTypeCommand(id1, dto), CancellationToken.None));
        }
    }

    [Fact]
    public async Task UpdateRequestType_SameNameOnSelf_Succeeds()
    {
        var dbName = Guid.NewGuid().ToString();
        int id;
        using (var context = CreateDbContext(dbName))
        {
            var rt = new RequestType { Name = "Leave A", Category = "Leave", Hint = "Original" };
            context.RequestTypes.Add(rt);
            await context.SaveChangesAsync();
            id = rt.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new UpdateRequestTypeCommandHandler(
                new GenericRepository<RequestType>(context),
                new UnitOfWork(context)
            );

            var dto = new UpdateRequestTypeDto("Leave", "Leave A", "Updated Hint", "Full", "Paid", true, true, true);
            var result = await handler.Handle(new UpdateRequestTypeCommand(id, dto), CancellationToken.None);

            Assert.Equal("Leave A", result.Name);
            Assert.Equal("Updated Hint", result.Hint);
        }
    }

    #endregion

    #region AC 4: Prevents deletion of request types that are referenced by existing employee submissions

    [Fact]
    public async Task DeleteRequestType_WhenUnused_DeletesSuccessfully()
    {
        var dbName = Guid.NewGuid().ToString();
        int id;
        using (var context = CreateDbContext(dbName))
        {
            var rt = new RequestType { Name = "Unused Request Type", Category = "General" };
            context.RequestTypes.Add(rt);
            await context.SaveChangesAsync();
            id = rt.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new DeleteRequestTypeCommandHandler(
                new GenericRepository<RequestType>(context),
                new GenericRepository<Request>(context),
                new UnitOfWork(context)
            );

            var result = await handler.Handle(new DeleteRequestTypeCommand(id), CancellationToken.None);
            Assert.True(result);

            var deleted = await context.RequestTypes.FindAsync(id);
            Assert.Null(deleted);
        }
    }

    [Fact]
    public async Task DeleteRequestType_WhenReferencedByEmployeeRequests_ThrowsInvalidOperationException()
    {
        var dbName = Guid.NewGuid().ToString();
        int id;
        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" };
            context.Employees.Add(emp);
            await context.SaveChangesAsync();

            var rt = new RequestType { Name = "Referenced Type", Category = "Leave" };
            context.RequestTypes.Add(rt);
            await context.SaveChangesAsync();
            id = rt.Id;

            var req = new Request
            {
                EmployeeId = emp.Id,
                RequestTypeId = rt.Id,
                Status = "Pending"
            };
            context.Requests.Add(req);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new DeleteRequestTypeCommandHandler(
                new GenericRepository<RequestType>(context),
                new GenericRepository<Request>(context),
                new UnitOfWork(context)
            );

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new DeleteRequestTypeCommand(id), CancellationToken.None));

            Assert.Equal("Cannot delete request type because it is referenced by existing employee requests.", ex.Message);

            // Verify entity was NOT deleted
            var stillExists = await context.RequestTypes.FindAsync(id);
            Assert.NotNull(stillExists);
        }
    }

    [Fact]
    public async Task DeleteRequestType_NonExistent_ThrowsKeyNotFoundException()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var handler = new DeleteRequestTypeCommandHandler(
                new GenericRepository<RequestType>(context),
                new GenericRepository<Request>(context),
                new UnitOfWork(context)
            );

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                handler.Handle(new DeleteRequestTypeCommand(999), CancellationToken.None));
        }
    }

    #endregion

    #region Controller Tests: Endpoints, Status Codes & Error Handling

    [Fact]
    public async Task Controller_GetRequestTypes_ReturnsOkWithList()
    {
        var sampleList = new List<RequestTypeDto>
        {
            new RequestTypeDto(1, "Leave", "Sick Leave", null, "Full", "Paid", true, true, true, DateTime.UtcNow, "Admin")
        };
        var mediator = new FakeSender(sampleList);

        var controller = new RequestTypesController(mediator);
        var actionResult = await controller.GetRequestTypes(null, null, null, null, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<RequestTypeDto>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task Controller_GetRequestTypeById_ReturnsOkWhenFound()
    {
        var dto = new RequestTypeDto(1, "Leave", "Sick Leave", null, "Full", "Paid", true, true, true, DateTime.UtcNow, "Admin");
        var mediator = new FakeSender(dto);

        var controller = new RequestTypesController(mediator);
        var actionResult = await controller.GetRequestTypeById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(dto, okResult.Value);
    }

    [Fact]
    public async Task Controller_GetRequestTypeById_Returns404WhenNotFound()
    {
        var mediator = new FakeSender(new KeyNotFoundException("Request type with ID 99 was not found."));

        var controller = new RequestTypesController(mediator);
        var actionResult = await controller.GetRequestTypeById(99, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    [Fact]
    public async Task Controller_CreateRequestType_Returns201Created()
    {
        var created = new RequestTypeDto(10, "Leave", "Annual Leave", null, "Full", "Paid", true, false, true, DateTime.UtcNow, "Admin");
        var mediator = new FakeSender(created);

        var controller = new RequestTypesController(mediator);
        var actionResult = await controller.CreateRequestType(
            new CreateRequestTypeDto("Leave", "Annual Leave", null, "Full", "Paid", true, false),
            CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task Controller_CreateRequestType_Returns400OnValidationException()
    {
        var mediator = new FakeSender(new ValidationException("Request type name is required."));

        var controller = new RequestTypesController(mediator);
        var actionResult = await controller.CreateRequestType(
            new CreateRequestTypeDto("Leave", "", null, null, null, false, false),
            CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Controller_UpdateRequestType_Returns200Ok()
    {
        var updated = new RequestTypeDto(5, "Leave", "Updated", null, "Full", "Paid", true, true, true, DateTime.UtcNow, "Admin");
        var mediator = new FakeSender(updated);

        var controller = new RequestTypesController(mediator);
        var actionResult = await controller.UpdateRequestType(5,
            new UpdateRequestTypeDto("Leave", "Updated", null, "Full", "Paid", true, true, true),
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(updated, okResult.Value);
    }

    [Fact]
    public async Task Controller_UpdateRequestType_Returns404OnKeyNotFound()
    {
        var mediator = new FakeSender(new KeyNotFoundException("Not found"));

        var controller = new RequestTypesController(mediator);
        var actionResult = await controller.UpdateRequestType(5,
            new UpdateRequestTypeDto("Leave", "Updated", null, null, null, false, false, true),
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    [Fact]
    public async Task Controller_DeleteRequestType_Returns204NoContent()
    {
        var mediator = new FakeSender(true);

        var controller = new RequestTypesController(mediator);
        var actionResult = await controller.DeleteRequestType(3, CancellationToken.None);

        Assert.IsType<NoContentResult>(actionResult);
    }

    [Fact]
    public async Task Controller_DeleteRequestType_Returns400WhenReferenced()
    {
        var mediator = new FakeSender(new InvalidOperationException("Cannot delete request type because it is referenced by existing employee requests."));

        var controller = new RequestTypesController(mediator);
        var actionResult = await controller.DeleteRequestType(3, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    #endregion

    private class FakeSender : ISender
    {
        private readonly object? _response;
        private readonly Exception? _exception;

        public FakeSender(object? response) => _response = response;
        public FakeSender(Exception exception) => _exception = exception;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (_exception != null) throw _exception;
            return Task.FromResult((TResponse)_response!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            if (_exception != null) throw _exception;
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            if (_exception != null) throw _exception;
            return Task.FromResult(_response);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
