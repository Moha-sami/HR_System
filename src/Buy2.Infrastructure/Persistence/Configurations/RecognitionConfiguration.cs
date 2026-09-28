using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class RecognitionConfiguration : IEntityTypeConfiguration<Recognition>
{
    public void Configure(EntityTypeBuilder<Recognition> builder)
    {
        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("nvarchar(200)");

        builder.Property(r => r.Narrative)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(r => r.Badge)
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.Property(r => r.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasColumnType("varchar(30)")
            .HasDefaultValue("Published");

        builder.HasOne(r => r.Author)
            .WithMany()
            .HasForeignKey(r => r.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Recipient)
            .WithMany()
            .HasForeignKey(r => r.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.HasIndex(r => r.RecipientId);
        builder.HasIndex(r => r.AuthorId);
        builder.HasIndex(r => r.Status);
    }
}
