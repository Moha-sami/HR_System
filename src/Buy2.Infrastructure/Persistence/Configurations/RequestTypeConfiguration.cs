using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class RequestTypeConfiguration : IEntityTypeConfiguration<RequestType>
{
    public void Configure(EntityTypeBuilder<RequestType> builder)
    {
        builder.Property(rt => rt.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(rt => rt.Category)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("nvarchar(50)");

        builder.Property(rt => rt.Hint)
            .HasMaxLength(250)
            .HasColumnType("nvarchar(250)");

        builder.Property(rt => rt.LeaveType)
            .HasMaxLength(20)
            .HasColumnType("nvarchar(20)");

        builder.Property(rt => rt.LeavePay)
            .HasMaxLength(20)
            .HasColumnType("nvarchar(20)");

        builder.Property(rt => rt.AddedBy)
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(rt => rt.IsActive)
            .HasDefaultValue(true);
    }
}
