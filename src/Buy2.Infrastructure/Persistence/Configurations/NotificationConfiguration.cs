using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("nvarchar(200)");

        builder.Property(n => n.Body)
            .IsRequired()
            .HasMaxLength(1000)
            .HasColumnType("nvarchar(1000)");

        builder.Property(n => n.Type)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.Property(n => n.ReferenceId)
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.Property(n => n.ReferenceType)
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.HasOne(n => n.Employee)
            .WithMany()
            .HasForeignKey(n => n.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
