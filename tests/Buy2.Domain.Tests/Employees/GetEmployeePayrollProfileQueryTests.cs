using Buy2.Application.Features.Employees.GetEmployeePayroll;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Employees;

public class GetEmployeePayrollProfileQueryTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenEmployeeIsDeletedOrNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var deletedEmp = new Employee
            {
                FirstName = "Deleted",
                LastName = "User",
                Email = "deleted@buy2.com",
                IsDeleted = true
            };
            context.Employees.Add(deletedEmp);
            await context.SaveChangesAsync();

            var empRepo = new GenericRepository<Employee>(context);
            var profileRepo = new GenericRepository<PayrollProfile>(context);
            var recordRepo = new GenericRepository<PayrollRecord>(context);

            var handler = new GetEmployeePayrollProfileQueryHandler(empRepo, profileRepo, recordRepo);

            var resultNotFound = await handler.Handle(new GetEmployeePayrollProfileQuery(999), CancellationToken.None);
            Assert.Null(resultNotFound);

            var resultDeleted = await handler.Handle(new GetEmployeePayrollProfileQuery(deletedEmp.Id), CancellationToken.None);
            Assert.Null(resultDeleted);
        }
    }

    [Fact]
    public async Task Handle_ReturnsEmptyPayrollRecords_WhenNoHistoryExists()
    {
        var dbName = Guid.NewGuid().ToString();
        int empId;

        using (var writeContext = CreateDbContext(dbName))
        {
            var emp = new Employee
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane.doe@buy2.com",
                IsDeleted = false
            };
            writeContext.Employees.Add(emp);
            await writeContext.SaveChangesAsync();
            empId = emp.Id;

            var profile = new PayrollProfile
            {
                EmployeeId = empId,
                SalaryType = "Fixed",
                PayoutPeriod = "Monthly",
                PayoutDay = 25,
                WorkWeekStart = "Sunday",
                WorkWeekEnd = "Thursday",
                PaymentAmount = 6000m,
                OvertimeThresholdHours = 40m,
                OvertimeHourlyRate = 25m,
                AttendanceType = "OnSite"
            };
            writeContext.PayrollProfiles.Add(profile);
            await writeContext.SaveChangesAsync();
        }

        using var readContext = CreateDbContext(dbName);
        var empRepo = new GenericRepository<Employee>(readContext);
        var profileRepo = new GenericRepository<PayrollProfile>(readContext);
        var recordRepo = new GenericRepository<PayrollRecord>(readContext);

        var handler = new GetEmployeePayrollProfileQueryHandler(empRepo, profileRepo, recordRepo);

        var result = await handler.Handle(new GetEmployeePayrollProfileQuery(empId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(empId, result.EmployeeId);
        Assert.True(result.IsConfigured);
        Assert.NotNull(result.PayrollRecords);
        Assert.Empty(result.PayrollRecords);
    }

    [Fact]
    public async Task Handle_ReturnsPayrollRecordsHistory_InDescendingOrder()
    {
        var dbName = Guid.NewGuid().ToString();
        int empId;
        var paidAtDate = new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc);

        using (var writeContext = CreateDbContext(dbName))
        {
            var emp = new Employee
            {
                FirstName = "John",
                LastName = "Smith",
                Email = "john.smith@buy2.com",
                IsDeleted = false
            };
            writeContext.Employees.Add(emp);
            await writeContext.SaveChangesAsync();
            empId = emp.Id;

            var recordJul = new PayrollRecord
            {
                EmployeeId = empId,
                PeriodYear = 2026,
                PeriodMonth = 7,
                BaseSalary = 5000m,
                OvertimePay = 200m,
                Bonuses = 300m,
                Deductions = 100m,
                NetSalary = 5400m,
                Status = "Paid",
                PayDate = paidAtDate.AddMonths(-1)
            };

            var recordAug = new PayrollRecord
            {
                EmployeeId = empId,
                PeriodYear = 2026,
                PeriodMonth = 8,
                BaseSalary = 5000m,
                OvertimePay = 400m,
                Bonuses = 500m,
                Deductions = 150m,
                NetSalary = 5750m,
                Status = "Paid",
                PayDate = paidAtDate
            };

            writeContext.PayrollRecords.AddRange(recordJul, recordAug);
            await writeContext.SaveChangesAsync();
        }

        using var readContext = CreateDbContext(dbName);
        var empRepo = new GenericRepository<Employee>(readContext);
        var profileRepo = new GenericRepository<PayrollProfile>(readContext);
        var recordRepo = new GenericRepository<PayrollRecord>(readContext);

        var handler = new GetEmployeePayrollProfileQueryHandler(empRepo, profileRepo, recordRepo);

        var result = await handler.Handle(new GetEmployeePayrollProfileQuery(empId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(empId, result.EmployeeId);
        Assert.NotNull(result.PayrollRecords);
        Assert.Equal(2, result.PayrollRecords.Count);

        // Verify descending order by Year/Month
        var firstRecord = result.PayrollRecords[0];
        Assert.Equal(2026, firstRecord.PeriodStartDate.Year);
        Assert.Equal(8, firstRecord.PeriodStartDate.Month);
        Assert.Equal(1, firstRecord.PeriodStartDate.Day);
        Assert.Equal(31, firstRecord.PeriodEndDate.Day);
        Assert.Equal(5000m, firstRecord.BaseSalary);
        Assert.Equal(400m, firstRecord.OvertimePay);
        Assert.Equal(500m, firstRecord.BonusPay);
        Assert.Equal(150m, firstRecord.Deductions);
        Assert.Equal(5750m, firstRecord.NetPay);
        Assert.Equal("Paid", firstRecord.PaymentStatus);
        Assert.Equal(paidAtDate, firstRecord.PaidAt);

        var secondRecord = result.PayrollRecords[1];
        Assert.Equal(2026, secondRecord.PeriodStartDate.Year);
        Assert.Equal(7, secondRecord.PeriodStartDate.Month);
        Assert.Equal(1, secondRecord.PeriodStartDate.Day);
        Assert.Equal(31, secondRecord.PeriodEndDate.Day);
        Assert.Equal(5400m, secondRecord.NetPay);
    }
}
