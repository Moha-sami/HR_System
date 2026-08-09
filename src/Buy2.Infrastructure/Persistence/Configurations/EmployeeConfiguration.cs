using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations
{
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
            builder.Property(p => p.PhoneNumber)
                .IsRequired(false)
                .HasMaxLength(20)
                .HasColumnType("varchar(20)");
            // Configure relationships for `JobRole`, `Role`, `Site`, and `AttendanceProfile` with `DeleteBehavior.Restrict`.
            builder.HasOne<JobRole>()
       .WithMany()
       .HasForeignKey(e => e.JobRoleId)
       .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Role>()
                   .WithMany()
                   .HasForeignKey(e => e.RoleId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Site>()
                   .WithMany()
                   .HasForeignKey(e => e.SiteId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
