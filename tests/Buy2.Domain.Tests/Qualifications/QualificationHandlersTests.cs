using Buy2.Application.Features.Qualifications.CreateQualification;
using Buy2.Application.Features.Qualifications.DTOs;
using Buy2.Application.Features.Qualifications.GetQualifications;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace Buy2.Domain.Tests.Qualifications;

public class QualificationHandlersTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetQualifications_ReturnsCatalogCategorizedByType()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            context.Qualifications.Add(new Qualification { Name = "BSc Computer Science", Category = "Degree", Description = "Bachelors degree" });
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetQualificationsQueryHandler(
                new Buy2.Infrastructure.Persistence.Repositories.GenericRepository<Qualification>(context)
            );

            var result = await handler.Handle(new GetQualificationsQuery(), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Single(result);
            var item = result.First();
            Assert.Equal("BSc Computer Science", item.Name);
            Assert.Equal("Degree", item.Type);
            Assert.Equal("Bachelors degree", item.Description);
            Assert.False(item.IsSystem);
        }
    }

    [Fact]
    public async Task CreateQualification_CreatesNewQualificationInline()
    {
        var dbName = Guid.NewGuid().ToString();
        
        using (var context = CreateDbContext(dbName))
        {
            var handler = new CreateQualificationCommandHandler(
                new Buy2.Infrastructure.Persistence.Repositories.GenericRepository<Qualification>(context),
                new Buy2.Infrastructure.Persistence.Repositories.UnitOfWork(context)
            );

            var dto = new CreateQualificationDto("AWS Certified", "Certification", "Cloud cert");
            var result = await handler.Handle(new CreateQualificationCommand(dto), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("AWS Certified", result.Name);
            Assert.Equal("Certification", result.Type);
            Assert.Equal("Cloud cert", result.Description);
            Assert.True(result.IsActive);
        }
    }

    [Fact]
    public async Task CreateQualification_DuplicateName_ThrowsInvalidOperationException()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            context.Qualifications.Add(new Qualification { Name = "Azure Expert", Category = "Certification" });
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new CreateQualificationCommandHandler(
                new Buy2.Infrastructure.Persistence.Repositories.GenericRepository<Qualification>(context),
                new Buy2.Infrastructure.Persistence.Repositories.UnitOfWork(context)
            );

            var dto = new CreateQualificationDto("Azure Expert", "Certification", null);
            
            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new CreateQualificationCommand(dto), CancellationToken.None));
        }
    }

    [Fact]
    public async Task GetQualifications_WithSearchAndTypeFilters_ReturnsMatchingQualifications()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            context.Qualifications.Add(new Qualification { Name = "AWS Solutions Architect", Category = "Certification", Description = "Cloud architecture" });
            context.Qualifications.Add(new Qualification { Name = "BSc Computer Science", Category = "Degree", Description = "Bachelors degree" });
            context.Qualifications.Add(new Qualification { Name = "MSc Data Science", Category = "Degree", Description = null }); // null description test
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var handler = new GetQualificationsQueryHandler(
                new Buy2.Infrastructure.Persistence.Repositories.GenericRepository<Qualification>(context)
            );

            // 1. Search matching Description
            var searchResult = await handler.Handle(new GetQualificationsQuery(Search: "architecture"), CancellationToken.None);
            Assert.Single(searchResult);
            Assert.Equal("AWS Solutions Architect", searchResult.First().Name);

            // 2. Search matching Name
            var nameResult = await handler.Handle(new GetQualificationsQuery(Search: "BSc"), CancellationToken.None);
            Assert.Single(nameResult);
            Assert.Equal("BSc Computer Science", nameResult.First().Name);

            // 3. Search matching Name where Description is null
            var nullDescResult = await handler.Handle(new GetQualificationsQuery(Search: "MSc"), CancellationToken.None);
            Assert.Single(nullDescResult);
            Assert.Equal("MSc Data Science", nullDescResult.First().Name);

            // 4. Search non-matching
            var noResult = await handler.Handle(new GetQualificationsQuery(Search: "notfound"), CancellationToken.None);
            Assert.Empty(noResult);

            // 5. Type matching Category case-insensitive
            var typeResult = await handler.Handle(new GetQualificationsQuery(Type: "DeGRee"), CancellationToken.None);
            Assert.Equal(2, typeResult.Count());

            // 6. Type non-matching
            var noTypeResult = await handler.Handle(new GetQualificationsQuery(Type: "Unknown"), CancellationToken.None);
            Assert.Empty(noTypeResult);
            
            // 7. Both empty/whitespace
            var allResult = await handler.Handle(new GetQualificationsQuery(Search: "   ", Type: "  "), CancellationToken.None);
            Assert.Equal(3, allResult.Count());
            
            // 8. Both Search and Type
            var bothResult = await handler.Handle(new GetQualificationsQuery(Search: "Science", Type: "Degree"), CancellationToken.None);
            Assert.Equal(2, bothResult.Count());
        }
    }
}
