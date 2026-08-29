using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class PointsAutomationSettingConfiguration : IEntityTypeConfiguration<PointsAutomationSetting>
{
    public void Configure(EntityTypeBuilder<PointsAutomationSetting> builder)
    {
        builder.Property(p => p.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.Property(p => p.SubCategory)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.Property(p => p.AutomationPeriod)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnType("varchar(20)");

        builder.Property(p => p.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.UpdatedAt)
            .HasColumnType("datetimeoffset");

        builder.HasIndex(p => new { p.Category, p.SubCategory }).IsUnique();

        builder.HasMany(p => p.Ranges)
            .WithOne(r => r.AutomationSetting)
            .HasForeignKey(r => r.AutomationSettingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}