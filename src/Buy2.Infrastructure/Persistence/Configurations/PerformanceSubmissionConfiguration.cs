using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class PerformanceSubmissionConfiguration : IEntityTypeConfiguration<PerformanceSubmission>
{
    public void Configure(EntityTypeBuilder<PerformanceSubmission> builder)
    {
        builder.Property(ps => ps.AchievedPercent)
            .HasColumnType("decimal(18,2)");

        builder.Property(ps => ps.Score)
            .HasColumnType("decimal(18,2)");

        builder.Property(ps => ps.Feedback)
            .HasMaxLength(1000)
            .HasColumnType("nvarchar(1000)");

        builder.HasOne(ps => ps.Employee)
            .WithMany()
            .HasForeignKey(ps => ps.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ps => ps.PerformanceMetric)
            .WithMany(pm => pm.PerformanceSubmissions)
            .HasForeignKey(ps => ps.MetricId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
