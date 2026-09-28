using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("nvarchar(200)");

        builder.Property(p => p.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.Category)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("varchar(50)")
            .HasDefaultValue("General");

        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasColumnType("varchar(30)")
            .HasDefaultValue("Draft");

        builder.Property(p => p.MediaUrl)
            .HasMaxLength(500)
            .HasColumnType("varchar(500)");

        builder.Property(p => p.PostType)
            .IsRequired()
            .HasMaxLength(30)
            .HasColumnType("varchar(30)");

        builder.HasOne(p => p.Author)
            .WithMany()
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.Category);
        builder.HasIndex(p => p.PublishedAt);
        builder.HasIndex(p => p.AuthorId);
    }
}
