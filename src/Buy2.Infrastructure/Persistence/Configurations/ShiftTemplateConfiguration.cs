using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class ShiftTemplateConfiguration : IEntityTypeConfiguration<ShiftTemplate>
{
    public void Configure(EntityTypeBuilder<ShiftTemplate> builder)
    {
        builder.Property(st => st.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(st => st.Location)
            .HasMaxLength(200)
            .HasColumnType("nvarchar(200)");

        builder.Property(st => st.DaysOfWeekJson)
            .HasColumnType("nvarchar(max)");

        builder.HasOne(st => st.Organization)
            .WithMany()
            .HasForeignKey(st => st.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
