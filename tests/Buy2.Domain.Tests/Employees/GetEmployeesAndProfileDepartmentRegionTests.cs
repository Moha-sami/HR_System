using Buy2.Application.Features.Employees.GetEmployee;
using Buy2.Application.Features.Employees.GetEmployees;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Employees;

public class GetEmployeesAndProfileDepartmentRegionTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetEmployeesQueryHandler_FiltersByDepartmentNameAndId()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using (var writeContext = CreateDbContext(dbName))
        {
            var org = new Organization { Name = "Main Org" };
            writeContext.Organizations.Add(org);
            await writeContext.SaveChangesAsync();

            var role = new Role { Name = "StandardUser", IsActive = true };
            writeContext.Roles.Add(role);
            await writeContext.SaveChangesAsync();

            var region = new Region { Name = "Headquarter Region" };
            writeContext.Regions.Add(region);
            await writeContext.SaveChangesAsync();

            var site = new Site { SiteName = "Default HQ", RegionId = region.Id };
            writeContext.Sites.Add(site);
            await writeContext.SaveChangesAsync();

            var deptEngineering = new Department { Name = "Engineering", OrganizationId = org.Id };
            var deptHR = new Department { Name = "Human Resources", OrganizationId = org.Id };
            writeContext.Departments.AddRange(deptEngineering, deptHR);
            await writeContext.SaveChangesAsync();

            var roleDev = new JobRole { Title = "Software Engineer", DepartmentId = deptEngineering.Id };
            var roleHR = new JobRole { Title = "HR Manager", DepartmentId = deptHR.Id };
            writeContext.JobRoles.AddRange(roleDev, roleHR);
            await writeContext.SaveChangesAsync();

            var emp1 = new Employee { FirstName = "Alice", LastName = "Smith", Email = "alice@buy2.com", JobRoleId = roleDev.Id, RoleId = role.Id, SiteId = site.Id };
            var emp2 = new Employee { FirstName = "Bob", LastName = "Jones", Email = "bob@buy2.com", JobRoleId = roleHR.Id, RoleId = role.Id, SiteId = site.Id };
            writeContext.Employees.AddRange(emp1, emp2);
            await writeContext.SaveChangesAsync();
        }

        using var readContext = CreateDbContext(dbName);
        var empRepo = new GenericRepository<Employee>(readContext);
        var handler = new GetEmployeesQueryHandler(empRepo);

        // Act - Filter by Department name string "Engine"
        var resultByName = await handler.Handle(new GetEmployeesQuery(Department: "Engine"), CancellationToken.None);

        // Act - Filter by Department integer ID
        var deptHrId = await readContext.Departments.Where(d => d.Name == "Human Resources").Select(d => d.Id).FirstAsync();
        var resultById = await handler.Handle(new GetEmployeesQuery(Department: deptHrId.ToString()), CancellationToken.None);

        // Assert
        Assert.Single(resultByName.Items);
        Assert.Equal("Alice Smith", resultByName.Items[0].EmployeeName);

        Assert.Single(resultById.Items);
        Assert.Equal("Bob Jones", resultById.Items[0].EmployeeName);
    }

    [Fact]
    public async Task GetEmployeesQueryHandler_FiltersByRegionNameAndId()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using (var writeContext = CreateDbContext(dbName))
        {
            var role = new Role { Name = "StandardUser", IsActive = true };
            writeContext.Roles.Add(role);
            await writeContext.SaveChangesAsync();

            var regionNorth = new Region { Name = "North America" };
            var regionEMEA = new Region { Name = "EMEA Region" };
            writeContext.Regions.AddRange(regionNorth, regionEMEA);
            await writeContext.SaveChangesAsync();

            var siteHQ = new Site { SiteName = "Headquarters", RegionId = regionNorth.Id };
            var siteBranch = new Site { SiteName = "London Office", RegionId = regionEMEA.Id };
            writeContext.Sites.AddRange(siteHQ, siteBranch);
            await writeContext.SaveChangesAsync();

            var devJobRole = new JobRole { Title = "Developer" };
            writeContext.JobRoles.Add(devJobRole);
            await writeContext.SaveChangesAsync();

            var emp1 = new Employee { FirstName = "Charlie", LastName = "Brown", Email = "charlie@buy2.com", SiteId = siteHQ.Id, RoleId = role.Id, JobRoleId = devJobRole.Id };
            var emp2 = new Employee { FirstName = "Diana", LastName = "Prince", Email = "diana@buy2.com", SiteId = siteBranch.Id, RoleId = role.Id, JobRoleId = devJobRole.Id };
            writeContext.Employees.AddRange(emp1, emp2);
            await writeContext.SaveChangesAsync();
        }

        using var readContext = CreateDbContext(dbName);
        var empRepo = new GenericRepository<Employee>(readContext);
        var handler = new GetEmployeesQueryHandler(empRepo);

        // Act - Filter by Region name string "EMEA"
        var resultByName = await handler.Handle(new GetEmployeesQuery(Region: "EMEA"), CancellationToken.None);

        // Act - Filter by Region integer ID
        var regionNorthId = await readContext.Regions.Where(r => r.Name == "North America").Select(r => r.Id).FirstAsync();
        var resultById = await handler.Handle(new GetEmployeesQuery(Region: regionNorthId.ToString()), CancellationToken.None);

        // Assert
        Assert.Single(resultByName.Items);
        Assert.Equal("Diana Prince", resultByName.Items[0].EmployeeName);

        Assert.Single(resultById.Items);
        Assert.Equal("Charlie Brown", resultById.Items[0].EmployeeName);
    }

    [Fact]
    public async Task GetEmployeeProfileQueryHandler_MapsDepartmentNameFromJobRoleDepartment()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        int empId;
        using (var writeContext = CreateDbContext(dbName))
        {
            var org = new Organization { Name = "Main Org" };
            writeContext.Organizations.Add(org);
            await writeContext.SaveChangesAsync();

            var role = new Role { Name = "StandardUser", IsActive = true };
            writeContext.Roles.Add(role);
            await writeContext.SaveChangesAsync();

            var region = new Region { Name = "Main Region" };
            writeContext.Regions.Add(region);
            await writeContext.SaveChangesAsync();

            var site = new Site { SiteName = "Headquarters", RegionId = region.Id };
            writeContext.Sites.Add(site);
            await writeContext.SaveChangesAsync();

            var deptFinance = new Department { Name = "Finance & Accounting", OrganizationId = org.Id };
            writeContext.Departments.Add(deptFinance);
            await writeContext.SaveChangesAsync();

            var roleAccountant = new JobRole { Title = "Senior Accountant", DepartmentId = deptFinance.Id };
            writeContext.JobRoles.Add(roleAccountant);
            await writeContext.SaveChangesAsync();

            var emp = new Employee
            {
                FirstName = "Edward",
                LastName = "Nygma",
                Email = "edward@buy2.com",
                JobRoleId = roleAccountant.Id,
                RoleId = role.Id,
                SiteId = site.Id
            };
            writeContext.Employees.Add(emp);
            await writeContext.SaveChangesAsync();
            empId = emp.Id;
        }

        using var readContext = CreateDbContext(dbName);
        var empRepo = new GenericRepository<Employee>(readContext);
        var pointsRepo = new GenericRepository<PointsTransaction>(readContext);
        var tasksRepo = new GenericRepository<EmployeeTask>(readContext);
        var redemptionsRepo = new GenericRepository<RewardRedemption>(readContext);

        var handler = new GetEmployeeProfileQueryHandler(empRepo, pointsRepo, tasksRepo, redemptionsRepo);

        // Act
        var result = await handler.Handle(new GetEmployeeProfileQuery(empId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Finance & Accounting", result.JobDetails.Department);
        Assert.Equal("Senior Accountant", result.JobDetails.Title);
    }

    [Fact]
    public async Task GetEmployeeProfileQueryHandler_ReturnsNAForDepartmentWhenJobRoleOrDepartmentIsNull()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        int empId;
        using (var writeContext = CreateDbContext(dbName))
        {
            var role = new Role { Name = "StandardUser", IsActive = true };
            writeContext.Roles.Add(role);
            await writeContext.SaveChangesAsync();

            var region = new Region { Name = "Main Region" };
            writeContext.Regions.Add(region);
            await writeContext.SaveChangesAsync();

            var site = new Site { SiteName = "Headquarters", RegionId = region.Id };
            writeContext.Sites.Add(site);
            await writeContext.SaveChangesAsync();

            var devJobRole = new JobRole { Title = "Developer", DepartmentId = null };
            writeContext.JobRoles.Add(devJobRole);
            await writeContext.SaveChangesAsync();

            var empNoDept = new Employee
            {
                FirstName = "Frank",
                LastName = "Castle",
                Email = "frank@buy2.com",
                RoleId = role.Id,
                SiteId = site.Id,
                JobRoleId = devJobRole.Id
            };
            writeContext.Employees.Add(empNoDept);
            await writeContext.SaveChangesAsync();
            empId = empNoDept.Id;
        }

        using var readContext = CreateDbContext(dbName);
        var empRepo = new GenericRepository<Employee>(readContext);
        var pointsRepo = new GenericRepository<PointsTransaction>(readContext);
        var tasksRepo = new GenericRepository<EmployeeTask>(readContext);
        var redemptionsRepo = new GenericRepository<RewardRedemption>(readContext);

        var handler = new GetEmployeeProfileQueryHandler(empRepo, pointsRepo, tasksRepo, redemptionsRepo);

        // Act
        var result = await handler.Handle(new GetEmployeeProfileQuery(empId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("N/A", result.JobDetails.Department);
    }
}
