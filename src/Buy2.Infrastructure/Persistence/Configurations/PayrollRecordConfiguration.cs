using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class PayrollRecordConfiguration : IEntityTypeConfiguration<PayrollRecord>
{
    public void Configure(EntityTypeBuilder<PayrollRecord> builder)
    {
        builder.Property(p => p.BaseSalary)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.OvertimePay)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.Bonuses)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.Deductions)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.NetSalary)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasColumnType("varchar(30)");

        builder.Property(p => p.PayslipUrl)
            .HasMaxLength(500)
            .HasColumnType("varchar(500)");

        builder.HasOne(p => p.Employee)
            .WithMany()
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
