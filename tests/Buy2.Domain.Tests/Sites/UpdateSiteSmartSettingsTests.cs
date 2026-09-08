using Buy2.Application.DTOs.Sites;
using Buy2.Application.Features.Sites.UpdateSiteSmartSettings;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Sites;

public class UpdateSiteSmartSettingsTests
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
    public async Task UpdateSiteSmartSettings_ExistingSite_UpdatesBothFlags_AndReturnsDto()
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
        var handler = new UpdateSiteSmartSettingsCommandHandler(siteRepo, uow);

        var command = new UpdateSiteSmartSettingsCommand(
            SiteId: 1,
            IsSmartAssignmentEnabled: false,
            IsSmartPostingEnabled: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.SiteId);
        Assert.False(result.IsSmartAssignmentEnabled);
        Assert.False(result.IsSmartPostingEnabled);

        var updatedSite = await context.Sites.FindAsync(1);
        Assert.NotNull(updatedSite);
        Assert.False(updatedSite.IsSmartAssignmentEnabled);
        Assert.False(updatedSite.IsSmartPostingEnabled);
    }

    [Fact]
    public async Task UpdateSiteSmartSettings_OnlyUpdatesSmartAssignment_LeavesSmartPostingUnchanged()
    {
        // Arrange
        using var context = CreateDbContext();
        var site = new Site
        {
            Id = 2,
            SiteName = "Smart Assignment Update Site",
            Address = "456 Avenue",
            IsSmartAssignmentEnabled = false,
            IsSmartPostingEnabled = true
        };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var uow = new UnitOfWork(context);
        var handler = new UpdateSiteSmartSettingsCommandHandler(siteRepo, uow);

        var command = new UpdateSiteSmartSettingsCommand(
            SiteId: 2,
            IsSmartAssignmentEnabled: true,
            IsSmartPostingEnabled: null
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.SiteId);
        Assert.True(result.IsSmartAssignmentEnabled);
        Assert.True(result.IsSmartPostingEnabled);

        var updatedSite = await context.Sites.FindAsync(2);
        Assert.NotNull(updatedSite);
        Assert.True(updatedSite.IsSmartAssignmentEnabled);
        Assert.True(updatedSite.IsSmartPostingEnabled);
    }

    [Fact]
    public async Task UpdateSiteSmartSettings_OnlyUpdatesSmartPosting_LeavesSmartAssignmentUnchanged()
    {
        // Arrange
        using var context = CreateDbContext();
        var site = new Site
        {
            Id = 3,
            SiteName = "Smart Posting Update Site",
            Address = "789 Boulevard",
            IsSmartAssignmentEnabled = true,
            IsSmartPostingEnabled = false
        };
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var siteRepo = new GenericRepository<Site>(context);
        var uow = new UnitOfWork(context);
        var handler = new UpdateSiteSmartSettingsCommandHandler(siteRepo, uow);

        var command = new UpdateSiteSmartSettingsCommand(
            SiteId: 3,
            IsSmartAssignmentEnabled: null,
            IsSmartPostingEnabled: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.SiteId);
        Assert.True(result.IsSmartAssignmentEnabled);
        Assert.True(result.IsSmartPostingEnabled);

        var updatedSite = await context.Sites.FindAsync(3);
        Assert.NotNull(updatedSite);
        Assert.True(updatedSite.IsSmartAssignmentEnabled);
        Assert.True(updatedSite.IsSmartPostingEnabled);
    }

    [Fact]
    public async Task UpdateSiteSmartSettings_NonExistentSite_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateDbContext();
        var siteRepo = new GenericRepository<Site>(context);
        var uow = new UnitOfWork(context);
        var handler = new UpdateSiteSmartSettingsCommandHandler(siteRepo, uow);

        var command = new UpdateSiteSmartSettingsCommand(
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
