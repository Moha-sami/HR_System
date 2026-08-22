using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.Property(a => a.HoursWorked)
            .HasColumnType("decimal(18,2)");

        builder.Property(a => a.OvertimeHours)
            .HasColumnType("decimal(18,2)");

        builder.Property(a => a.BreakMinutes)
            .HasColumnType("decimal(18,2)");

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasColumnType("varchar(30)");

        builder.Property(a => a.LeaveType)
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(a => a.Notes)
            .HasMaxLength(500)
            .HasColumnType("nvarchar(500)");

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
