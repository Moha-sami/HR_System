using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class PayrollProfileConfiguration : IEntityTypeConfiguration<PayrollProfile>
{
    public void Configure(EntityTypeBuilder<PayrollProfile> builder)
    {
        builder.Property(p => p.PaymentAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.OvertimeThresholdHours)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.OvertimeHourlyRate)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.PayoutPeriod)
            .HasMaxLength(50)
            .HasColumnType("nvarchar(50)");

        builder.Property(p => p.SalaryType)
            .HasConversion<string>()
            .HasColumnType("varchar(20)");

        builder.HasOne(p => p.Employee)
            .WithOne(e => e.PayrollProfile)
            .HasForeignKey<PayrollProfile>(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
