using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(2000)
            .HasColumnType("nvarchar(2000)");

        builder.Property(c => c.ModerationReason)
            .HasMaxLength(500)
            .HasColumnType("nvarchar(500)");

        builder.HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ParentComment)
            .WithMany(p => p.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasIndex(c => c.PostId);
        builder.HasIndex(c => c.ParentCommentId);
        builder.HasIndex(c => c.AuthorId);
    }
}
