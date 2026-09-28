using Buy2.Application.DTOs.Recognitions;
using Buy2.Application.Features.Recognitions.Common;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Buy2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Buy2.Domain.Tests.Recognitions;

public class RecognitionFoundationTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Theory]
    [InlineData("Draft", "Published")]
    [InlineData("Published", "Archived")]
    [InlineData("Draft", "Archived")]
    public void ValidateTransition_ValidTransitions_ReturnsSuccess(string current, string target)
    {
        var (isValid, error) = RecognitionLifecycleManager.ValidateTransition(current, target, null);
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateTransition_ScheduledWithFutureDate_ReturnsSuccess()
    {
        var future = DateTime.UtcNow.AddDays(2);
        var (isValid, error) = RecognitionLifecycleManager.ValidateTransition("Draft", "Scheduled", future);
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateTransition_ScheduledWithPastDate_ReturnsError()
    {
        var past = DateTime.UtcNow.AddMinutes(-10);
        var (isValid, error) = RecognitionLifecycleManager.ValidateTransition("Draft", "Scheduled", past);
        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateTransition_ScheduledWithoutDate_ReturnsError()
    {
        var (isValid, error) = RecognitionLifecycleManager.ValidateTransition("Draft", "Scheduled", null);
        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateTransition_InvalidStatus_ReturnsError()
    {
        var (isValid, error) = RecognitionLifecycleManager.ValidateTransition("Draft", "NonExistentStatus", null);
        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Fact]
    public void ResolveEffectiveStatus_ScheduledInPast_ReturnsPublished()
    {
        var past = DateTime.UtcNow.AddMinutes(-5);
        var result = RecognitionLifecycleManager.ResolveEffectiveStatus("Scheduled", past);
        Assert.Equal("Published", result);
    }

    [Fact]
    public void ResolveEffectiveStatus_ScheduledInFuture_ReturnsScheduled()
    {
        var future = DateTime.UtcNow.AddHours(3);
        var result = RecognitionLifecycleManager.ResolveEffectiveStatus("Scheduled", future);
        Assert.Equal("Scheduled", result);
    }

    [Theory]
    [InlineData("Excellence", true)]
    [InlineData("TeamPlayer", true)]
    [InlineData("Innovator", true)]
    [InlineData("Leadership", true)]
    [InlineData("CustomerChampion", true)]
    [InlineData("ProblemSolver", true)]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("UnknownBadge", false)]
    public void IsValidBadge_Validation_ReturnsExpected(string? badge, bool expected)
    {
        var result = RecognitionLifecycleManager.IsValidBadge(badge);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ValidatePointsGrant_ValidGrant_ReturnsSuccess()
    {
        var (isValid, error) = RecognitionLifecycleManager.ValidatePointsGrant(250, authorId: 1, recipientId: 2);
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void ValidatePointsGrant_SelfRecognition_ReturnsError()
    {
        var (isValid, error) = RecognitionLifecycleManager.ValidatePointsGrant(100, authorId: 5, recipientId: 5);
        Assert.False(isValid);
        Assert.Contains("themselves", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidatePointsGrant_NegativePoints_ReturnsError()
    {
        var (isValid, error) = RecognitionLifecycleManager.ValidatePointsGrant(-50, authorId: 1, recipientId: 2);
        Assert.False(isValid);
        Assert.Contains("negative", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidatePointsGrant_ExceedsMaxPoints_ReturnsError()
    {
        var (isValid, error) = RecognitionLifecycleManager.ValidatePointsGrant(5001, authorId: 1, recipientId: 2);
        Assert.False(isValid);
        Assert.Contains("maximum", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CalculatePointsAdjustment_IncreasedPoints_ReturnsAddDelta()
    {
        var (delta, type) = RecognitionPointsGrantEngine.CalculatePointsAdjustment(100, 250);
        Assert.Equal(150, delta);
        Assert.Equal(TransactionType.Add, type);
    }

    [Fact]
    public void CalculatePointsAdjustment_DecreasedPoints_ReturnsDeductDelta()
    {
        var (delta, type) = RecognitionPointsGrantEngine.CalculatePointsAdjustment(300, 100);
        Assert.Equal(200, delta);
        Assert.Equal(TransactionType.Deduct, type);
    }

    [Fact]
    public void CalculatePointsAdjustment_UnchangedPoints_ReturnsZero()
    {
        var (delta, type) = RecognitionPointsGrantEngine.CalculatePointsAdjustment(100, 100);
        Assert.Equal(0, delta);
        Assert.Null(type);
    }

    [Fact]
    public void CreateGrantTransaction_ConstructsValidPointsTransaction()
    {
        var tx = RecognitionPointsGrantEngine.CreateGrantTransaction(recipientId: 10, points: 500, recognitionId: 42, authorId: 3);

        Assert.Equal(10, tx.EmployeeId);
        Assert.Equal(500, tx.Amount);
        Assert.Equal(TransactionType.Add, tx.TransactionType);
        Assert.Equal(3, tx.CreatedByUserId);
        Assert.Equal(RecognitionPointsGrantEngine.TriggeredByRecognition, tx.TriggeredBy);
    }

    [Fact]
    public void CreateRollbackTransaction_ConstructsValidPointsTransaction()
    {
        var tx = RecognitionPointsGrantEngine.CreateRollbackTransaction(recipientId: 10, points: 500, recognitionId: 42, operatorId: 1);

        Assert.Equal(10, tx.EmployeeId);
        Assert.Equal(500, tx.Amount);
        Assert.Equal(TransactionType.Deduct, tx.TransactionType);
        Assert.Equal(1, tx.CreatedByUserId);
        Assert.Equal(RecognitionPointsGrantEngine.TriggeredByRollback, tx.TriggeredBy);
    }

    [Fact]
    public async Task RecognitionEntity_CanBePersistedAndQueriedWithAuthorAndRecipient()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Manager", LastName = "User", Email = "mgr@corp.com" };
            var recipient = new Employee { FirstName = "Star", LastName = "Performer", Email = "star@corp.com" };
            context.Employees.AddRange(author, recipient);
            await context.SaveChangesAsync();

            var recognition = new Recognition
            {
                AuthorId = author.Id,
                RecipientId = recipient.Id,
                Title = "Q3 Outstanding Achievement",
                Narrative = "Delivered critical infrastructure project ahead of schedule.",
                Badge = RecognitionLifecycleManager.BadgeExcellence,
                AwardedPoints = 750,
                Status = RecognitionLifecycleManager.StatusPublished,
                PublishedAt = DateTime.UtcNow
            };
            context.Recognitions.Add(recognition);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var saved = await context.Recognitions
                .Include(r => r.Author)
                .Include(r => r.Recipient)
                .FirstOrDefaultAsync();

            Assert.NotNull(saved);
            Assert.Equal("Q3 Outstanding Achievement", saved.Title);
            Assert.Equal(750, saved.AwardedPoints);
            Assert.Equal("Manager User", $"{saved.Author!.FirstName} {saved.Author.LastName}".Trim());
            Assert.Equal("Star Performer", $"{saved.Recipient!.FirstName} {saved.Recipient.LastName}".Trim());
        }
    }
}
