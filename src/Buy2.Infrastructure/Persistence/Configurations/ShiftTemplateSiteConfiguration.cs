using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class ShiftTemplateSiteConfiguration : IEntityTypeConfiguration<ShiftTemplateSite>
{
    public void Configure(EntityTypeBuilder<ShiftTemplateSite> builder)
    {
        builder.HasOne(x => x.ShiftTemplate)
            .WithMany(st => st.ShiftTemplateSites)
            .HasForeignKey(x => x.ShiftTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Site)
            .WithMany()
            .HasForeignKey(x => x.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ShiftTemplateId, x.SiteId })
            .IsUnique();
    }
}
