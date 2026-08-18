using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class DisciplinaryViolationConfiguration : IEntityTypeConfiguration<DisciplinaryViolation>
{
    public void Configure(EntityTypeBuilder<DisciplinaryViolation> builder)
    {
        builder.Property(d => d.Severity)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnType("varchar(20)");

        builder.Property(d => d.Description)
            .IsRequired()
            .HasMaxLength(1000)
            .HasColumnType("nvarchar(1000)");

        builder.Property(d => d.ViolationType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasColumnType("varchar(30)");

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasColumnType("varchar(30)");

        builder.Property(d => d.WitnessesJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(d => d.DocumentUrl)
            .HasMaxLength(500)
            .HasColumnType("varchar(500)");

        builder.Property(d => d.ActionType)
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(d => d.ActionDescription)
            .HasMaxLength(1000)
            .HasColumnType("nvarchar(1000)");

        // Foreign key relations with Restrict delete behavior
        builder.HasOne(d => d.Employee)
            .WithMany()
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ReportedBy)
            .WithMany()
            .HasForeignKey(d => d.ReportedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ActionTakenBy)
            .WithMany()
            .HasForeignKey(d => d.ActionTakenById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}