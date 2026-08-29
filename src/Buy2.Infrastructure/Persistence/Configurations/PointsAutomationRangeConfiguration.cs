using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class PointsAutomationRangeConfiguration : IEntityTypeConfiguration<PointsAutomationRange>
{
    public void Configure(EntityTypeBuilder<PointsAutomationRange> builder)
    {
        builder.Property(p => p.RangeType)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnType("varchar(20)");

        builder.Property(p => p.FromValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.ToValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.TaskPriority)
            .HasMaxLength(20)
            .HasColumnType("varchar(20)");

        builder.Property(p => p.PointsValue)
            .IsRequired()
            .HasColumnType("int");

        builder.Property(p => p.UpdatedAt)
            .HasColumnType("datetimeoffset");

        builder.HasOne(p => p.AutomationSetting)
            .WithMany(s => s.Ranges)
            .HasForeignKey(p => p.AutomationSettingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}