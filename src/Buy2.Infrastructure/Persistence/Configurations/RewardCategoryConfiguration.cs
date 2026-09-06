using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;
public class RewardCategoryConfiguration : IEntityTypeConfiguration<RewardCategory>
{
    public void Configure(EntityTypeBuilder<RewardCategory> builder)
    {
        builder.Property(e => e.Name)
            .IsRequired()
            .HasColumnType("nvarchar(100)");

        builder.HasMany(e => e.RewardItems)
            .WithOne()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}