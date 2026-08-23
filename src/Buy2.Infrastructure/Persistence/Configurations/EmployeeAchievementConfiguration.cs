using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class EmployeeAchievementConfiguration : IEntityTypeConfiguration<EmployeeAchievement>
{
    public void Configure(EntityTypeBuilder<EmployeeAchievement> builder)
    {
        builder.Property(ea => ea.BadgeType)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.HasOne(ea => ea.Employee)
            .WithMany(e => e.Achievements)
            .HasForeignKey(ea => ea.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ea => ea.Badge)
            .WithMany()
            .HasForeignKey(ea => ea.BadgeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
