using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;
public class ShiftConfiguration : IEntityTypeConfiguration<ShiftEntity>
{
    public void Configure(EntityTypeBuilder<ShiftEntity> builder)
    {
        builder.Property(s => s.StartTime)
            .IsRequired()
            .HasColumnType("datetimeoffset");
        builder.Property(s => s.EndTime)
            .IsRequired()
            .HasColumnType("datetimeoffset");
        builder.Property(s => s.IsPublished)
            .IsRequired();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Site>()
            .WithMany()
            .HasForeignKey(s => s.SiteId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<JobRole>()
            .WithMany()
            .HasForeignKey(s => s.JobRoleId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
