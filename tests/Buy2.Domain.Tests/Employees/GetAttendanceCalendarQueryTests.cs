using Buy2.Application.Features.Employees.GetAttendanceCalendar;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Employees;

public class GetAttendanceCalendarQueryTests
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
        using var context = CreateDbContext(dbName);

        var deletedEmp = new Employee { FirstName = "Ghost", LastName = "User", IsDeleted = true };
        context.Employees.Add(deletedEmp);
        await context.SaveChangesAsync();

        var handler = new GetAttendanceCalendarQueryHandler(
            new GenericRepository<Employee>(context),
            new GenericRepository<AttendanceRecord>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Request>(context)
        );

        var nullResult = await handler.Handle(new GetAttendanceCalendarQuery(9999), CancellationToken.None);
        Assert.Null(nullResult);

        var deletedResult = await handler.Handle(new GetAttendanceCalendarQuery(deletedEmp.Id), CancellationToken.None);
        Assert.Null(deletedResult);
    }

    [Fact]
    public async Task Handle_EagerLoadsShiftsAndMapsApprovedLeaveRequests()
    {
        var dbName = Guid.NewGuid().ToString();
        int empId;
        int targetYear = 2026;
        int targetMonth = 8;

        using (var writeContext = CreateDbContext(dbName))
        {
            var emp = new Employee
            {
                FirstName = "Alice",
                LastName = "Smith",
                Email = "alice.smith@buy2.com",
                IsDeleted = false
            };
            writeContext.Employees.Add(emp);
            await writeContext.SaveChangesAsync();
            empId = emp.Id;

            // Scheduled shift on August 10, 2026 (10 hours shift: 08:00 to 18:00 UTC)
            var shift = new ShiftEntity
            {
                EmployeeId = empId,
                SiteId = 1,
                JobRoleId = 1,
                StartTime = new DateTimeOffset(2026, 8, 10, 8, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2026, 8, 10, 18, 0, 0, TimeSpan.Zero),
                IsPublished = true
            };
            writeContext.ShiftEntities.Add(shift);
            await writeContext.SaveChangesAsync();

            // Attendance record linked to ScheduledShiftId on August 10, 2026
            var record = new AttendanceRecord
            {
                EmployeeId = empId,
                Date = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc),
                ScheduledShiftId = shift.Id,
                ScheduledShift = shift,
                ClockInTime = new DateTimeOffset(2026, 8, 10, 8, 0, 0, TimeSpan.Zero),
                ClockOutTime = new DateTimeOffset(2026, 8, 10, 18, 0, 0, TimeSpan.Zero),
                HoursWorked = 10.0m,
                BreakMinutes = 60.0m,
                Status = AttendanceDayStatus.OnTime
            };
            writeContext.AttendanceRecords.Add(record);
            await writeContext.SaveChangesAsync();

            // Approved Leave Request from August 17 to August 18, 2026
            var requestType = new RequestType { Name = "Sick Leave", Category = "TimeOff", RequiresDates = true };
            writeContext.RequestTypes.Add(requestType);
            await writeContext.SaveChangesAsync();

            var leaveRequest = new Request
            {
                EmployeeId = empId,
                RequestTypeId = requestType.Id,
                RequestType = requestType,
                StartDate = new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 8, 18, 23, 59, 59, DateTimeKind.Utc),
                Status = "Approved",
                Reason = "Flu symptoms"
            };
            writeContext.Requests.Add(leaveRequest);
            await writeContext.SaveChangesAsync();
        }

        using var readContext = CreateDbContext(dbName);
        var handler = new GetAttendanceCalendarQueryHandler(
            new GenericRepository<Employee>(readContext),
            new GenericRepository<AttendanceRecord>(readContext),
            new GenericRepository<ShiftEntity>(readContext),
            new GenericRepository<Request>(readContext)
        );

        // Act
        var result = await handler.Handle(new GetAttendanceCalendarQuery(empId, Month: targetMonth, Year: targetYear), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(31, result.Days.Count);

        // Check August 10 shift mapping
        var shiftDay = result.Days.FirstOrDefault(d => d.Date.Day == 10);
        Assert.NotNull(shiftDay);
        Assert.Equal(AttendanceDayStatus.OnTime, shiftDay.Status);
        Assert.Equal(10.0m, shiftDay.HoursWorked);
        Assert.Equal(60.0m, shiftDay.BreakTime);
        Assert.Equal(0.0m, shiftDay.HoursLeft);

        // Check August 17 & 18 leave mapping
        var leaveDay1 = result.Days.FirstOrDefault(d => d.Date.Day == 17);
        Assert.NotNull(leaveDay1);
        Assert.Equal(AttendanceDayStatus.ApprovedLeave, leaveDay1.Status);
        Assert.Equal("Sick Leave", leaveDay1.LeaveType);
        Assert.Equal(0.0m, leaveDay1.HoursLeft);

        var leaveDay2 = result.Days.FirstOrDefault(d => d.Date.Day == 18);
        Assert.NotNull(leaveDay2);
        Assert.Equal(AttendanceDayStatus.ApprovedLeave, leaveDay2.Status);
        Assert.Equal("Sick Leave", leaveDay2.LeaveType);
        Assert.Equal(0.0m, leaveDay2.HoursLeft);
    }
}
