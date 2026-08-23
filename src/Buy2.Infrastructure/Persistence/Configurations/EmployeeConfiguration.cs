using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("nvarchar(50)");

        builder.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("nvarchar(50)");

        builder.HasIndex(e => e.Email)
            .IsUnique();

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnType("varchar(150)");

        builder.Property(e => e.PhoneNumber)
            .IsRequired(false)
            .HasMaxLength(20)
            .HasColumnType("varchar(20)");

        builder.Property(e => e.EmployeeCode)
            .HasMaxLength(50)
            .HasColumnType("nvarchar(50)");

        builder.Property(e => e.Gender)
            .HasConversion<string>()
            .HasColumnType("varchar(20)");

        builder.Property(e => e.ProfilePhotoUrl)
            .HasMaxLength(500)
            .HasColumnType("varchar(500)");

        builder.Property(e => e.Address)
            .HasMaxLength(250)
            .HasColumnType("nvarchar(250)");

        builder.Property(e => e.EmergencyContact)
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(e => e.NationalId)
            .HasMaxLength(50)
            .HasColumnType("nvarchar(50)");

        builder.Property(e => e.SeniorityLevel)
            .HasMaxLength(50)
            .HasColumnType("nvarchar(50)");

        builder.Property(e => e.JobType)
            .HasMaxLength(50)
            .HasColumnType("nvarchar(50)");

        builder.Property(e => e.AttendanceType)
            .HasMaxLength(50)
            .HasColumnType("nvarchar(50)");

        builder.Property(e => e.OnlineWorkdaysJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.OfflineWorkdaysJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.PasswordHash)
            .HasColumnType("nvarchar(max)");

        // Foreign Key Relationships
        builder.HasOne(e => e.JobRole)
            .WithMany()
            .HasForeignKey(e => e.JobRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Role)
            .WithMany(r => r.Employees)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Site)
            .WithMany()
            .HasForeignKey(e => e.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referential DirectManager relation with Restrict delete behavior
        builder.HasOne(e => e.DirectManager)
            .WithMany()
            .HasForeignKey(e => e.DirectManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1:1 relation with PayrollProfile (Cascade on employee delete)
        builder.HasOne(e => e.PayrollProfile)
            .WithOne(p => p.Employee)
            .HasForeignKey<PayrollProfile>(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.WorkSites);
    }
}
