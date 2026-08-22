using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class SiteOperationalHourConfiguration : IEntityTypeConfiguration<SiteOperationalHour>
{
    public void Configure(EntityTypeBuilder<SiteOperationalHour> builder)
    {
        builder.Property(s => s.DayOfWeek)
           .IsRequired();
        builder.Property(s => s.IsOpen)
            .IsRequired();
        builder.Property(s => s.OpenTime)
            .IsRequired();
        builder.Property(s => s.CloseTime)
            .IsRequired();

        builder.HasOne<Site>()
           .WithMany()
           .HasForeignKey(s => s.SiteId)
           .OnDelete(DeleteBehavior.Cascade);
    }
}