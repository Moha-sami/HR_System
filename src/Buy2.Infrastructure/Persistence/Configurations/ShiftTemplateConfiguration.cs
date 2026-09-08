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

        builder.HasOne(st => st.Organization)
            .WithMany()
            .HasForeignKey(st => st.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(st => st.LastUpdatedByEmployee)
            .WithMany()
            .HasForeignKey(st => st.LastUpdatedByEmployeeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(st => st.ShiftBlocks)
            .WithOne(b => b.ShiftTemplate)
            .HasForeignKey(b => b.ShiftTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(st => st.ShiftTemplateSites)
            .WithOne(x => x.ShiftTemplate)
            .HasForeignKey(x => x.ShiftTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
