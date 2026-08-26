using Buy2.Application.DTOs.Sites;
using Buy2.Application.Features.Sites.DeleteSite;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Sites;

public class SiteDeletionTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    #region CheckSiteDeletionQuery Tests

    [Fact]
    public async Task CheckSiteDeletion_NonExistentSite_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var siteRepo = new GenericRepository<Site>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var handler = new CheckSiteDeletionQueryHandler(siteRepo, empRepo);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new CheckSiteDeletionQuery(999), CancellationToken.None));
    }

    [Fact]
    public async Task CheckSiteDeletion_SiteWithNoEmployeesOrShifts_ReturnsCanDeleteTrue()
    {
        // Arrange
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Cairo HQ" };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var handler = new CheckSiteDeletionQueryHandler(siteRepo, empRepo);

        // Act
        var result = await handler.Handle(new CheckSiteDeletionQuery(1), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CanDelete);
        Assert.Equal(0, result.AllocatedEmployeesCount);
        Assert.Empty(result.AllocatedEmployees);
    }

    [Fact]
    public async Task CheckSiteDeletion_SiteWithAllocatedEmployees_ReturnsCanDeleteFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Cairo HQ" };
        var emp1 = new Employee { Id = 10, FirstName = "Ahmed", LastName = "Ali", Email = "ahmed@test.com", SiteId = 1 };
        context.Sites.Add(site);
        context.Employees.Add(emp1);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var handler = new CheckSiteDeletionQueryHandler(siteRepo, empRepo);

        // Act
        var result = await handler.Handle(new CheckSiteDeletionQuery(1), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.CanDelete);
        Assert.Equal(1, result.AllocatedEmployeesCount);
        Assert.Single(result.AllocatedEmployees);
        Assert.Equal(10, result.AllocatedEmployees[0].EmployeeId);
        Assert.Equal("Ahmed Ali", result.AllocatedEmployees[0].FullName);
    }

    [Fact]
    public async Task CheckSiteDeletion_SiteWithFutureShifts_ReturnsCanDeleteFalse()
    {
        // Arrange
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Cairo HQ" };
        var shift = new ShiftEntity
        {
            Id = 5,
            SiteId = 1,
            StartTime = DateTimeOffset.UtcNow.AddDays(2),
            EndTime = DateTimeOffset.UtcNow.AddDays(2).AddHours(8)
        };
        site.Shifts.Add(shift);
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var handler = new CheckSiteDeletionQueryHandler(siteRepo, empRepo);

        // Act
        var result = await handler.Handle(new CheckSiteDeletionQuery(1), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.CanDelete);
    }

    #endregion

    #region DeleteSiteCommand Tests

    [Fact]
    public async Task DeleteSite_NonExistentSite_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var siteRepo = new GenericRepository<Site>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var uow = new UnitOfWork(context);
        var handler = new DeleteSiteCommandHandler(siteRepo, empRepo, uow);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new DeleteSiteCommand(999, null), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteSite_SiteWithFutureShifts_ThrowsValidationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Cairo HQ" };
        var shift = new ShiftEntity
        {
            Id = 5,
            SiteId = 1,
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(8)
        };
        site.Shifts.Add(shift);
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var uow = new UnitOfWork(context);
        var handler = new DeleteSiteCommandHandler(siteRepo, empRepo, uow);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new DeleteSiteCommand(1, null), CancellationToken.None));
        Assert.Contains("future scheduled shifts", ex.Message);
    }

    [Fact]
    public async Task DeleteSite_SiteWithAllocatedEmployeesNoReallocation_ThrowsValidationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Cairo HQ" };
        var emp = new Employee { Id = 10, FirstName = "Sara", LastName = "Hassan", Email = "sara@test.com", SiteId = 1 };
        context.Sites.Add(site);
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var uow = new UnitOfWork(context);
        var handler = new DeleteSiteCommandHandler(siteRepo, empRepo, uow);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new DeleteSiteCommand(1, null), CancellationToken.None));
        Assert.Contains("reallocated before deleting", ex.Message);
    }

    [Fact]
    public async Task DeleteSite_ReallocateToDeletedSite_ThrowsValidationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Cairo HQ" };
        var emp = new Employee { Id = 10, FirstName = "Sara", LastName = "Hassan", Email = "sara@test.com", SiteId = 1 };
        context.Sites.Add(site);
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var uow = new UnitOfWork(context);
        var handler = new DeleteSiteCommandHandler(siteRepo, empRepo, uow);

        var reassignments = new List<EmployeeSiteReassignmentDto>
        {
            new EmployeeSiteReassignmentDto(10, 1) // targeting same site being deleted
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new DeleteSiteCommand(1, reassignments), CancellationToken.None));
        Assert.Contains("cannot be reallocated to the site being deleted", ex.Message);
    }

    [Fact]
    public async Task DeleteSite_ReallocateToNonExistentSite_ThrowsValidationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Cairo HQ" };
        var emp = new Employee { Id = 10, FirstName = "Sara", LastName = "Hassan", Email = "sara@test.com", SiteId = 1 };
        context.Sites.Add(site);
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var uow = new UnitOfWork(context);
        var handler = new DeleteSiteCommandHandler(siteRepo, empRepo, uow);

        var reassignments = new List<EmployeeSiteReassignmentDto>
        {
            new EmployeeSiteReassignmentDto(10, 999) // site 999 does not exist
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new DeleteSiteCommand(1, reassignments), CancellationToken.None));
        Assert.Contains("replacement sites do not exist", ex.Message);
    }

    [Fact]
    public async Task DeleteSite_ValidReallocationAndDeletion_ReassignsEmployeesAndDeletesSite()
    {
        // Arrange
        using var context = CreateDbContext();
        var site1 = new Site { Id = 1, SiteName = "Old Site" };
        var site2 = new Site { Id = 2, SiteName = "New Site" };
        var emp1 = new Employee { Id = 10, FirstName = "Omar", LastName = "Khaled", Email = "omar@test.com", SiteId = 1 };
        var emp2 = new Employee { Id = 11, FirstName = "Mona", LastName = "Zaki", Email = "mona@test.com", SiteId = 1 };

        context.Sites.AddRange(site1, site2);
        context.Employees.AddRange(emp1, emp2);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var uow = new UnitOfWork(context);
        var handler = new DeleteSiteCommandHandler(siteRepo, empRepo, uow);

        var reassignments = new List<EmployeeSiteReassignmentDto>
        {
            new EmployeeSiteReassignmentDto(10, 2),
            new EmployeeSiteReassignmentDto(11, 2)
        };

        // Act
        await handler.Handle(new DeleteSiteCommand(1, reassignments), CancellationToken.None);

        // Assert
        var deletedSite = await context.Sites.FindAsync(1);
        Assert.Null(deletedSite);

        var updatedEmp1 = await context.Employees.FindAsync(10);
        var updatedEmp2 = await context.Employees.FindAsync(11);
        Assert.NotNull(updatedEmp1);
        Assert.NotNull(updatedEmp2);
        Assert.Equal(2, updatedEmp1.SiteId);
        Assert.Equal(2, updatedEmp2.SiteId);
    }

    [Fact]
    public async Task DeleteSite_EmptySite_DeletesDirectly()
    {
        // Arrange
        using var context = CreateDbContext();
        var site = new Site { Id = 1, SiteName = "Empty Site" };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var uow = new UnitOfWork(context);
        var handler = new DeleteSiteCommandHandler(siteRepo, empRepo, uow);

        // Act
        await handler.Handle(new DeleteSiteCommand(1, null), CancellationToken.None);

        // Assert
        var deletedSite = await context.Sites.FindAsync(1);
        Assert.Null(deletedSite);
    }

    #endregion
}
