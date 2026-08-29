using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class PointsRuleConfiguration : IEntityTypeConfiguration<PointsRule>
{
    public void Configure(EntityTypeBuilder<PointsRule> builder)
    {
        builder.Property(p => p.RuleKey)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("varchar(100)");

        builder.Property(p => p.EventType)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.Property(p => p.ConditionExpression)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnType("varchar(500)");

        builder.Property(p => p.ActionType)
            .IsRequired()
            .HasMaxLength(30)
            .HasColumnType("varchar(30)");

        builder.Property(p => p.PointValue)
            .IsRequired()
            .HasColumnType("int");

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.UpdatedAt)
            .HasColumnType("datetimeoffset");

        builder.HasIndex(p => p.RuleKey).IsUnique();
    }
}