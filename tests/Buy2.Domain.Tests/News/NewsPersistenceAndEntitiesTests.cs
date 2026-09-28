using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Buy2.Domain.Tests.News;

public class NewsPersistenceAndEntitiesTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task Post_CreationAndSoftDelete_FiltersOutDeletedPosts()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var author = new Employee { FirstName = "Marcus", LastName = "Fenix", Email = "marcus@cog.org" };
            context.Employees.Add(author);
            await context.SaveChangesAsync();

            var activePost = new Post
            {
                AuthorId = author.Id,
                Title = "Victory at Aspho Fields",
                Content = "Detailed report on operations.",
                Category = "Announcement",
                Status = "Published",
                PostType = "News",
                PublishedAt = DateTime.UtcNow
            };

            var deletedPost = new Post
            {
                AuthorId = author.Id,
                Title = "Archived Intel",
                Content = "Redacted content.",
                Category = "General",
                Status = "Archived",
                PostType = "News",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow
            };

            context.Posts.AddRange(activePost, deletedPost);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var posts = await context.Posts.ToListAsync();
            Assert.Single(posts);
            Assert.Equal("Victory at Aspho Fields", posts[0].Title);

            var allIncludingDeleted = await context.Posts.IgnoreQueryFilters().ToListAsync();
            Assert.Equal(2, allIncludingDeleted.Count);
        }
    }

    [Fact]
    public async Task Recognition_PersistsRecipientPointsAndNarrative()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var giver = new Employee { FirstName = "Sarah", LastName = "Connor", Email = "sarah@rebel.org" };
            var receiver = new Employee { FirstName = "Kyle", LastName = "Reese", Email = "kyle@rebel.org" };
            context.Employees.AddRange(giver, receiver);
            await context.SaveChangesAsync();

            var recognition = new Recognition
            {
                AuthorId = giver.Id,
                RecipientId = receiver.Id,
                AwardedPoints = 150,
                Title = "Courage Under Fire",
                Narrative = "Outstanding bravery during the night patrol mission.",
                Badge = "Valor",
                Status = "Published",
                PublishedAt = DateTime.UtcNow
            };

            context.Recognitions.Add(recognition);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var rec = await context.Recognitions
                .Include(r => r.Author)
                .Include(r => r.Recipient)
                .FirstOrDefaultAsync();

            Assert.NotNull(rec);
            Assert.Equal("Courage Under Fire", rec.Title);
            Assert.Equal(150, rec.AwardedPoints);
            Assert.Equal("Sarah", rec.Author?.FirstName);
            Assert.Equal("Kyle", rec.Recipient?.FirstName);
        }
    }

    [Fact]
    public async Task Comment_SupportsParentChildThreadedReplies()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
            context.Employees.Add(emp);
            await context.SaveChangesAsync();

            var post = new Post
            {
                AuthorId = emp.Id,
                Title = "Team Briefing",
                Content = "All-hands briefing at 10 AM.",
                Category = "Meeting",
                Status = "Published",
                PostType = "News"
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var parentComment = new Comment
            {
                PostId = post.Id,
                AuthorId = emp.Id,
                Content = "Will there be a dial-in link?"
            };
            context.Comments.Add(parentComment);
            await context.SaveChangesAsync();

            var reply = new Comment
            {
                PostId = post.Id,
                AuthorId = emp.Id,
                ParentCommentId = parentComment.Id,
                Content = "Yes, Zoom link is in the calendar invite."
            };
            context.Comments.Add(reply);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var comments = await context.Comments
                .Include(c => c.Replies)
                .Where(c => c.ParentCommentId == null)
                .ToListAsync();

            Assert.Single(comments);
            Assert.Equal("Will there be a dial-in link?", comments[0].Content);
            Assert.Single(comments[0].Replies);
            Assert.Equal("Yes, Zoom link is in the calendar invite.", comments[0].Replies.First().Content);
        }
    }

    [Fact]
    public async Task Reaction_PersistsPolymorphicTargetAndReactionType()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var context = CreateDbContext(dbName))
        {
            var emp = new Employee { FirstName = "Alice", LastName = "Walker", Email = "alice@example.com" };
            context.Employees.Add(emp);
            await context.SaveChangesAsync();

            var post = new Post
            {
                AuthorId = emp.Id,
                Title = "Product Launch",
                Content = "V2 is live in production.",
                Category = "Product",
                Status = "Published",
                PostType = "News"
            };
            context.Posts.Add(post);
            await context.SaveChangesAsync();

            var reaction = new Reaction
            {
                PostId = post.Id,
                TargetType = "Post",
                TargetId = post.Id,
                EmployeeId = emp.Id,
                ReactionType = "Celebrate"
            };
            context.Reactions.Add(reaction);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var r = await context.Reactions.FirstOrDefaultAsync();
            Assert.NotNull(r);
            Assert.Equal("Celebrate", r.ReactionType);
            Assert.Equal("Post", r.TargetType);
        }
    }
}
