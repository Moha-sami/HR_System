using Buy2.Domain.Entities;
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
            .HasColumnType("varchar(30)");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PointsRule>()
            .WithMany()
            .HasForeignKey(p => p.PointsRuleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}