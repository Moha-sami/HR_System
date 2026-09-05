using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class PointsAutomationRunConfiguration : IEntityTypeConfiguration<PointsAutomationRun>
{
    public void Configure(EntityTypeBuilder<PointsAutomationRun> builder)
    {
        builder.Property(p => p.AutomationPeriod)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnType("varchar(20)");

        builder.Property(p => p.PeriodStart)
            .IsRequired()
            .HasColumnType("datetimeoffset");

        builder.Property(p => p.PeriodEnd)
            .IsRequired()
            .HasColumnType("datetimeoffset");

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnType("varchar(20)");

        builder.Property(p => p.ExecutedAt)
            .IsRequired()
            .HasColumnType("datetimeoffset");

        builder.Property(p => p.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasIndex(p => new { p.AutomationPeriod, p.PeriodStart, p.PeriodEnd })
            .IsUnique()
            .HasFilter("[Status] = 'Completed'")
            .HasDatabaseName("IX_PointsAutomationRun_Period");
    }
}
