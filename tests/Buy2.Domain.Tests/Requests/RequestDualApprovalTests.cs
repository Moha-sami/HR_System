using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Buy2.Domain.Tests.Requests;

public class RequestDualApprovalTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task Request_PersistsDualApprovalFieldsAndAttachments()
    {
        var dbName = Guid.NewGuid().ToString();

        int requestId;
        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
            var manager = new Employee { FirstName = "Jane", LastName = "Manager", Email = "manager@example.com" };
            var hr = new Employee { FirstName = "Alice", LastName = "HR", Email = "hr@example.com" };
            var reqType = new RequestType { Name = "Remote Work", Category = "General" };

            context.Employees.AddRange(emp, manager, hr);
            context.RequestTypes.Add(reqType);
            await context.SaveChangesAsync();

            var request = new Request
            {
                EmployeeId = emp.Id,
                RequestTypeId = reqType.Id,
                ManagerId = manager.Id,
                HrId = hr.Id,
                Status = "Pending",
                ManagerStatus = "Pending",
                HrStatus = "Pending",
                CategoryValuesJson = "{\"location\":\"Home\",\"days\":3}",
                SubmittedAt = DateTime.UtcNow
            };

            request.Attachments.Add(new RequestAttachment
            {
                FileName = "proof.pdf",
                StorageUrl = "https://storage.example.com/proof.pdf",
                ContentType = "application/pdf",
                FileSize = 10240,
                UploadedAt = DateTime.UtcNow
            });

            context.Requests.Add(request);
            await context.SaveChangesAsync();
            requestId = request.Id;
        }

        using (var context = CreateDbContext(dbName))
        {
            var loaded = await context.Requests
                .Include(r => r.Attachments)
                .Include(r => r.Manager)
                .Include(r => r.Hr)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            Assert.NotNull(loaded);
            Assert.Equal("Pending", loaded.Status);
            Assert.Equal("Pending", loaded.ManagerStatus);
            Assert.Equal("Pending", loaded.HrStatus);
            Assert.Equal("Jane", loaded.Manager!.FirstName);
            Assert.Equal("Alice", loaded.Hr!.FirstName);
            Assert.Single(loaded.Attachments);
            Assert.Equal("proof.pdf", loaded.Attachments.First().FileName);
            Assert.Contains("Home", loaded.CategoryValuesJson);
        }
    }
}
