using Buy2.Application.Features.Sites.UpdateSiteAutomationSettings;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Sites;

public class UpdateSiteAutomationSettingsTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task UpdateSiteAutomationSettings_ExistingSite_UpdatesBothFlags()
    {
        // Arrange
        using var context = CreateDbContext();
        var site = new Site
        {
            Id = 1,
            SiteName = "Test Site",
            Address = "123 Street",
            IsSmartAssignmentEnabled = true,
            IsSmartPostingEnabled = true
        };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var uow = new UnitOfWork(context);
        var handler = new UpdateSiteAutomationSettingsCommandHandler(siteRepo, uow);

        var command = new UpdateSiteAutomationSettingsCommand(
            SiteId: 1,
            IsSmartAssignmentEnabled: false,
            IsSmartPostingEnabled: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var updatedSite = await context.Sites.FindAsync(1);
        Assert.NotNull(updatedSite);
        Assert.False(updatedSite.IsSmartAssignmentEnabled);
        Assert.False(updatedSite.IsSmartPostingEnabled);
    }

    [Fact]
    public async Task UpdateSiteAutomationSettings_PartialUpdate_OnlyUpdatesSpecifiedFlag()
    {
        // Arrange
        using var context = CreateDbContext();
        var site = new Site
        {
            Id = 2,
            SiteName = "Partial Update Site",
            Address = "456 Avenue",
            IsSmartAssignmentEnabled = true,
            IsSmartPostingEnabled = true
        };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var uow = new UnitOfWork(context);
        var handler = new UpdateSiteAutomationSettingsCommandHandler(siteRepo, uow);

        // Act: Update only smart posting to false
        var command = new UpdateSiteAutomationSettingsCommand(
            SiteId: 2,
            IsSmartAssignmentEnabled: null,
            IsSmartPostingEnabled: false
        );
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        var updatedSite = await context.Sites.FindAsync(2);
        Assert.NotNull(updatedSite);
        Assert.True(updatedSite.IsSmartAssignmentEnabled); // unchanged
        Assert.False(updatedSite.IsSmartPostingEnabled);   // updated
    }

    [Fact]
    public async Task UpdateSiteAutomationSettings_NonExistentSite_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var siteRepo = new GenericRepository<Site>(context);
        var uow = new UnitOfWork(context);
        var handler = new UpdateSiteAutomationSettingsCommandHandler(siteRepo, uow);

        var command = new UpdateSiteAutomationSettingsCommand(
            SiteId: 999,
            IsSmartAssignmentEnabled: false,
            IsSmartPostingEnabled: false
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("999", exception.Message);
    }
}
