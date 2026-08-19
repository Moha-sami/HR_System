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

        builder.HasOne<Site>()
            .WithMany()
            .HasForeignKey(x => x.SiteId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}