using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnType("varchar(150)");

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20)
            .HasColumnType("varchar(20)");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(u => u.OtpCode)
            .HasMaxLength(10)
            .HasColumnType("varchar(10)");

        builder.HasOne(u => u.Employee)
            .WithMany()
            .HasForeignKey(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
