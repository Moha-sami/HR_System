using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;
public class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.Property(r => r.Name)
          .IsRequired()
          .HasColumnType("nvarchar(100)");
        builder.HasIndex(r => r.Name)
            .IsUnique();
        builder.Property(r => r.Description)
          .IsRequired(false)
          .HasColumnType("nvarchar(500)");

        builder.HasMany(r => r.Sites)
            .WithOne(s => s.Region)
            .HasForeignKey(s => s.RegionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}