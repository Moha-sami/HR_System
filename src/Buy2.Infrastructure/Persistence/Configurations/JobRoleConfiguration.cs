using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class JobRoleConfiguration : IEntityTypeConfiguration<JobRole>
{
    public void Configure(EntityTypeBuilder<JobRole> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Title)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(j => j.Description)
            .HasMaxLength(1000);

        builder.Property(j => j.SeniorityLevel)
            .HasMaxLength(50)
            .HasDefaultValue("Junior");

        builder.Property(j => j.AttendanceType)
            .HasMaxLength(50)
            .HasDefaultValue("OnSite");

        builder.HasIndex(j => new { j.Title, j.DepartmentId });

        builder.HasOne(j => j.Department)
            .WithMany(d => d.JobRoles)
            .HasForeignKey(j => j.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(j => j.Employees)
            .WithOne(e => e.JobRole)
            .HasForeignKey(e => e.JobRoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
