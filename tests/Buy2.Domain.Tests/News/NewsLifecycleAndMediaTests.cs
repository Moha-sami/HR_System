using Buy2.Application.Common.Utilities;
using Buy2.Application.DTOs.News;
using Buy2.Application.Features.News.Common;
using System;
using Xunit;

namespace Buy2.Domain.Tests.News;

public class NewsLifecycleAndMediaTests
{
    [Fact]
    public void ValidateMedia_AcceptsValidImageWithin10MB()
    {
        long validSize = 5 * 1024 * 1024; // 5 MB
        var (isValid, error) = NewsMediaValidator.ValidateMedia(validSize, "announcement.png", "image/png");

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateMedia_RejectsFileExceeding10MB()
    {
        long oversized = 11 * 1024 * 1024; // 11 MB
        var (isValid, error) = NewsMediaValidator.ValidateMedia(oversized, "video.mp4", "video/mp4");

        Assert.False(isValid);
        Assert.Contains("exceeds the 10 MB limit", error);
    }

    [Fact]
    public void ValidateMedia_RejectsEmptyOrZeroByteFile()
    {
        var (isValid, error) = NewsMediaValidator.ValidateMedia(0, "empty.jpg", "image/jpeg");

        Assert.False(isValid);
        Assert.Contains("cannot be empty", error);
    }

    [Fact]
    public void ValidateMedia_RejectsUnsupportedExtension()
    {
        long validSize = 1024;
        var (isValid, error) = NewsMediaValidator.ValidateMedia(validSize, "executable.exe", "application/octet-stream");

        Assert.False(isValid);
        Assert.Contains("Unsupported media file format", error);
    }

    [Fact]
    public void NewsLifecycle_ValidatesFutureDateForScheduledPosts()
    {
        var pastDate = DateTime.UtcNow.AddMinutes(-10);
        var (isValidPast, errorPast) = NewsLifecycleManager.ValidateTransition("Draft", "Scheduled", pastDate);
        Assert.False(isValidPast);
        Assert.Contains("must be in the future", errorPast);

        var (isValidNoDate, errorNoDate) = NewsLifecycleManager.ValidateTransition("Draft", "Scheduled", null);
        Assert.False(isValidNoDate);
        Assert.Contains("requires a future release timestamp", errorNoDate);

        var futureDate = DateTime.UtcNow.AddHours(2);
        var (isValidFuture, errorFuture) = NewsLifecycleManager.ValidateTransition("Draft", "Scheduled", futureDate);
        Assert.True(isValidFuture);
        Assert.Null(errorFuture);
    }

    [Fact]
    public void NewsLifecycle_ResolvesScheduledPostToPublishedWhenDateHasPassed()
    {
        var pastDate = DateTime.UtcNow.AddSeconds(-5);
        var status = NewsLifecycleManager.ResolveEffectiveStatus(NewsLifecycleManager.StatusScheduled, pastDate);

        Assert.Equal(NewsLifecycleManager.StatusPublished, status);
    }

    [Fact]
    public void NewsLifecycle_KeepsScheduledWhenDateIsInFuture()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var status = NewsLifecycleManager.ResolveEffectiveStatus(NewsLifecycleManager.StatusScheduled, futureDate);

        Assert.Equal(NewsLifecycleManager.StatusScheduled, status);
    }
}
