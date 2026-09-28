using Buy2.Application.DTOs.News;
using Buy2.Application.Features.News.Common;
using Buy2.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Buy2.Domain.Tests.News;

public class SocialEngagementFoundationTests
{
    [Theory]
    [InlineData("like", "Like")]
    [InlineData("LIKE", "Like")]
    [InlineData("Dislike", "Dislike")]
    [InlineData("laugh", "Laugh")]
    [InlineData("WOW", "Wow")]
    [InlineData("heart", "Heart")]
    [InlineData("angry", "Angry")]
    public void NormalizeReactionType_ValidTypes_NormalizesCorrectly(string input, string expected)
    {
        var result = SocialEngagementManager.NormalizeReactionType(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("thumbsup")]
    [InlineData("sad")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IsValidReactionType_InvalidTypes_ReturnsFalse(string? input)
    {
        var isValid = SocialEngagementManager.IsValidReactionType(input);
        Assert.False(isValid);
    }

    [Fact]
    public void NormalizeReactionType_InvalidType_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SocialEngagementManager.NormalizeReactionType("unsupported"));
    }

    [Fact]
    public void CalculateReactionToggle_NoExistingReaction_AddsNewReaction()
    {
        var (newReaction, wasRemoved, wasSwitched, wasAdded) =
            SocialEngagementManager.CalculateReactionToggle(null, "Heart");

        Assert.Equal("Heart", newReaction);
        Assert.False(wasRemoved);
        Assert.False(wasSwitched);
        Assert.True(wasAdded);
    }

    [Fact]
    public void CalculateReactionToggle_SameReaction_RemovesReaction()
    {
        var (newReaction, wasRemoved, wasSwitched, wasAdded) =
            SocialEngagementManager.CalculateReactionToggle("Heart", "heart");

        Assert.Null(newReaction);
        Assert.True(wasRemoved);
        Assert.False(wasSwitched);
        Assert.False(wasAdded);
    }

    [Fact]
    public void CalculateReactionToggle_DifferentReaction_SwitchesReactionMutuallyExclusive()
    {
        var (newReaction, wasRemoved, wasSwitched, wasAdded) =
            SocialEngagementManager.CalculateReactionToggle("Like", "Wow");

        Assert.Equal("Wow", newReaction);
        Assert.False(wasRemoved);
        Assert.True(wasSwitched);
        Assert.False(wasAdded);
    }

    [Fact]
    public void ResolveCommentDisplayContent_ActiveComment_ReturnsOriginalContent()
    {
        var content = "This is a great update!";
        var result = SocialEngagementManager.ResolveCommentDisplayContent(content, isModerated: false, isDeleted: false);

        Assert.Equal(content, result);
    }

    [Fact]
    public void ResolveCommentDisplayContent_ModeratedComment_ReturnsModerationTombstone()
    {
        var content = "Inappropriate text";
        var result = SocialEngagementManager.ResolveCommentDisplayContent(content, isModerated: true, isDeleted: false);

        Assert.Equal(SocialEngagementManager.TombstoneModeratedComment, result);
    }

    [Fact]
    public void ResolveCommentDisplayContent_DeletedComment_ReturnsDeletedTombstone()
    {
        var content = "Author deleted text";
        var result = SocialEngagementManager.ResolveCommentDisplayContent(content, isModerated: false, isDeleted: true);

        Assert.Equal(SocialEngagementManager.TombstoneDeletedComment, result);
    }

    [Fact]
    public void ThreadedCommentDtoHierarchy_CanBeConstructedProperly()
    {
        var reply = new CommentReplyDto(
            Id: 2,
            PostId: 10,
            ParentCommentId: 1,
            AuthorId: 20,
            AuthorName: "Colleague User",
            AuthorAvatar: null,
            Content: "I agree with this point",
            IsModerated: false,
            ModerationReason: null,
            LikesCount: 3,
            ReactionBreakdown: new Dictionary<string, int> { { "Like", 2 }, { "Heart", 1 } },
            UserReaction: "Heart",
            CreatedAt: DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt: null
        );

        var parent = new CommentThreadDto(
            Id: 1,
            PostId: 10,
            ParentCommentId: null,
            AuthorId: 15,
            AuthorName: "First Author",
            AuthorAvatar: null,
            Content: "Top-level comment",
            IsModerated: false,
            ModerationReason: null,
            LikesCount: 5,
            ReactionBreakdown: new Dictionary<string, int> { { "Like", 5 } },
            UserReaction: "Like",
            Replies: new List<CommentReplyDto> { reply },
            CreatedAt: DateTime.UtcNow.AddHours(-1),
            UpdatedAt: null
        );

        Assert.Equal(1, parent.Id);
        Assert.Null(parent.ParentCommentId);
        Assert.Single(parent.Replies);
        Assert.Equal(1, parent.Replies[0].ParentCommentId);
        Assert.Equal("Colleague User", parent.Replies[0].AuthorName);
    }
}
