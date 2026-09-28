using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.Property(r => r.ReactionType)
            .IsRequired()
            .HasMaxLength(30)
            .HasColumnType("varchar(30)");

        builder.Property(r => r.TargetType)
            .IsRequired()
            .HasMaxLength(30)
            .HasColumnType("varchar(30)")
            .HasDefaultValue("Post");

        builder.HasOne(r => r.Post)
            .WithMany(p => p.Reactions)
            .HasForeignKey(r => r.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Comment)
            .WithMany(c => c.Reactions)
            .HasForeignKey(r => r.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Recognition)
            .WithMany(rg => rg.Reactions)
            .HasForeignKey(r => r.RecognitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.EmployeeId, r.TargetType, r.TargetId })
            .IsUnique();

        builder.HasIndex(r => r.PostId);
        builder.HasIndex(r => r.CommentId);
        builder.HasIndex(r => r.RecognitionId);
    }
}
