using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class EmployeeSiteConfiguration : IEntityTypeConfiguration<EmployeeSite>
{
    public void Configure(EntityTypeBuilder<EmployeeSite> builder)
    {
        builder.HasKey(es => new { es.EmployeeId, es.SiteId });

        builder.HasOne(es => es.Employee)
            .WithMany(e => e.EmployeeSites)
            .HasForeignKey(es => es.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(es => es.Site)
            .WithMany(s => s.EmployeeSites)
            .HasForeignKey(es => es.SiteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
