using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class SitePreferredEmployeeConfiguration : IEntityTypeConfiguration<SitePreferredEmployee>
{
    public void Configure(EntityTypeBuilder<SitePreferredEmployee> builder)
    {
        builder.HasKey(x => new
        {
            x.SiteId,
            x.EmployeeId
        });

        builder.HasOne(x => x.Site)
            .WithMany(s => s.PreferredEmployees)
            .HasForeignKey(x => x.SiteId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}