using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class PointsTransactionConfiguration : IEntityTypeConfiguration<PointsTransaction>
{
    public void Configure(EntityTypeBuilder<PointsTransaction> builder)
    {
        builder.Property(p => p.Amount)
            .IsRequired()
            .HasColumnType("int");

        builder.Property(p => p.TransactionType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasColumnType("varchar(30)");

        builder.Property(p => p.TriggeredBy)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("varchar(100)");

        builder.Property(p => p.Comments)
            .HasMaxLength(1000)
            .HasColumnType("nvarchar(1000)");

        builder.Property(p => p.EvaluationPeriodStart)
            .HasColumnType("datetimeoffset");

        builder.Property(p => p.EvaluationPeriodEnd)
            .HasColumnType("datetimeoffset");

        builder.Property(p => p.AutomationCategory)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasColumnType("varchar(30)");

        builder.Property(p => p.CreatedByUserId)
            .HasColumnType("int");

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasColumnType("datetimeoffset");

        builder.HasOne(p => p.Employee)
            .WithMany()
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.PointsRule)
            .WithMany()
            .HasForeignKey(p => p.PointsRuleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => new { p.EmployeeId, p.CreatedAt });
        builder.HasIndex(p => p.TriggeredBy);
        builder.HasIndex(p => p.TransactionType);
        
        builder.HasIndex(p => new { p.EmployeeId, p.AutomationCategory, p.TriggeredBy, p.EvaluationPeriodStart, p.EvaluationPeriodEnd })
            .IsUnique()
            .HasDatabaseName("IX_PointsTransaction_Idempotency");
    }
}