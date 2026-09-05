using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class EmployeeTaskConfiguration : IEntityTypeConfiguration<EmployeeTask>
{
    public void Configure(EntityTypeBuilder<EmployeeTask> builder)
    {
        builder.Property(et => et.TaskName)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("nvarchar(200)");

        builder.Property(et => et.Status)
            .HasConversion<string>()
            .HasColumnType("varchar(30)");

        builder.Property(et => et.Priority)
            .HasMaxLength(20)
            .HasColumnType("varchar(20)")
            .HasDefaultValue("Medium");

        builder.HasOne(et => et.Employee)
            .WithMany(e => e.Tasks)
            .HasForeignKey(et => et.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
