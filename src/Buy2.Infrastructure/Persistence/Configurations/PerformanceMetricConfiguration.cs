using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class PerformanceMetricConfiguration : IEntityTypeConfiguration<PerformanceMetric>
{
    public void Configure(EntityTypeBuilder<PerformanceMetric> builder)
    {
        builder.Property(pm => pm.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(pm => pm.Description)
            .HasMaxLength(500)
            .HasColumnType("nvarchar(500)");

        builder.Property(pm => pm.Target)
            .HasColumnType("decimal(18,2)");

        builder.Property(pm => pm.Weight)
            .HasColumnType("decimal(18,2)");

        builder.Property(pm => pm.CurrentScore)
            .HasColumnType("decimal(18,2)");

        builder.Property(pm => pm.AllTimeAverage)
            .HasColumnType("decimal(18,2)");

        builder.HasMany(pm => pm.PerformanceSubmissions)
            .WithOne(ps => ps.PerformanceMetric)
            .HasForeignKey(ps => ps.MetricId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
