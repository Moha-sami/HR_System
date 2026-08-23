using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(b => b.IconUrl)
            .HasMaxLength(500)
            .HasColumnType("varchar(500)");

        builder.Property(b => b.Description)
            .HasMaxLength(500)
            .HasColumnType("nvarchar(500)");

        builder.Property(b => b.TriggerType)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.Property(b => b.TriggerThreshold)
            .HasColumnType("decimal(18,2)");
    }
}
