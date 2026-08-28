using Buy2.Application.Features.Departments.CreateDepartment;
using Buy2.Application.Features.Departments.DTOs;
using Buy2.Application.Features.Departments.GetDepartments;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace Buy2.Domain.Tests.Departments;

public class DepartmentHandlersTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetDepartments_ReturnsLightweightLookupList()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "John", LastName = "Doe", EmployeeCode = "EMP001", Email = "john@example.com" };
            context.Employees.Add(emp);
            await context.SaveChangesAsync();

            var dept = new Department { Name = "HR", Description = "HR Dept", HeadEmployeeId = emp.Id };
            context.Departments.Add(dept);
            await context.SaveChangesAsync();
            
            var job1 = new JobRole { Title = "Job1", DepartmentId = dept.Id, IsActive = true };
            var job2 = new JobRole { Title = "Job2", DepartmentId = dept.Id, IsActive = true };
            context.JobRoles.AddRange(job1, job2);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetDepartmentsQueryHandler(
                new Buy2.Infrastructure.Persistence.Repositories.GenericRepository<Department>(context)
            );

            var result = await handler.Handle(new GetDepartmentsQuery(), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Single(result);
            var item = result.First();
            Assert.Equal("HR", item.Name);
            Assert.Equal("HR Dept", item.Description);
            Assert.Equal(2, item.ActiveJobCount);
            Assert.Equal("John Doe", item.HeadEmployeeName);
        }
    }

    [Fact]
    public async Task GetDepartments_WithNullHeadEmployeeAndInactiveJobs_MapsCorrectly()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var dept = new Department { Name = "Finance", Description = "Finance Dept", HeadEmployeeId = null };
            context.Departments.Add(dept);
            await context.SaveChangesAsync();
            
            var job1 = new JobRole { Title = "Job1", DepartmentId = dept.Id, IsActive = true };
            var job2 = new JobRole { Title = "Job2", DepartmentId = dept.Id, IsActive = false };
            context.JobRoles.AddRange(job1, job2);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetDepartmentsQueryHandler(
                new Buy2.Infrastructure.Persistence.Repositories.GenericRepository<Department>(context)
            );

            var result = await handler.Handle(new GetDepartmentsQuery(), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Single(result);
            var item = result.First();
            Assert.Equal("Finance", item.Name);
            Assert.Null(item.HeadEmployeeName);
            Assert.Equal(1, item.ActiveJobCount); // Should only count active jobs
        }
    }

    [Fact]
    public async Task CreateDepartment_CreatesNewDepartmentInline()
    {
        var dbName = Guid.NewGuid().ToString();
        
        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "Jane", LastName = "Smith", EmployeeCode = "EMP002", Email = "jane@example.com" };
            context.Employees.Add(emp);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new CreateDepartmentCommandHandler(
                new Buy2.Infrastructure.Persistence.Repositories.GenericRepository<Department>(context),
                new Buy2.Infrastructure.Persistence.Repositories.UnitOfWork(context)
            );

            var dto = new CreateDepartmentDto("IT", "IT-01", "IT Dept", 1);
            var result = await handler.Handle(new CreateDepartmentCommand(dto), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("IT", result.Name);
            Assert.Equal("IT Dept", result.Description);
            Assert.Equal(1, result.HeadEmployeeId);
            Assert.Null(result.HeadEmployeeName);
            Assert.Equal("IT-01", result.Code);
            Assert.Equal(0, result.JobRolesCount);
            Assert.Equal(0, result.EmployeesCount);
            Assert.True(result.IsActive);

            var savedDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "IT");
            Assert.NotNull(savedDept);
            Assert.NotEqual(default, savedDept.CreatedAt);
        }
    }

    [Fact]
    public async Task CreateDepartment_DuplicateName_ThrowsInvalidOperationException()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            context.Departments.Add(new Department { Name = "Finance" });
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new CreateDepartmentCommandHandler(
                new Buy2.Infrastructure.Persistence.Repositories.GenericRepository<Department>(context),
                new Buy2.Infrastructure.Persistence.Repositories.UnitOfWork(context)
            );

            var dto = new CreateDepartmentDto("Finance", null, "Finance Dept", null);
            
            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new CreateDepartmentCommand(dto), CancellationToken.None));
        }
    }
}
