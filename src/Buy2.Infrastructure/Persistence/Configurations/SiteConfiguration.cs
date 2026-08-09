using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Buy2.Infrastructure.Persistence.Configurations
{
    public class SiteConfiguration : IEntityTypeConfiguration<Site>
    {
        public void Configure(EntityTypeBuilder<Site> builder)
        {
            builder.Property(s => s.SiteName)
                .IsRequired()
                .HasColumnType("nvarchar(100)");

            builder.Property(s => s.Latitude)
                .IsRequired()
                .HasPrecision(9, 6);

            builder.Property(s => s.Longitude)
               .IsRequired()
               .HasPrecision(9, 6);

            builder.Property(s => s.MacAddressWhitelistJson)
                .IsRequired(false)
                .HasColumnType("nvarchar(max)");

            builder.HasMany(s => s.Employees)
               .WithOne(e => e.Site)
               .HasForeignKey(e => e.SiteId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Shifts)
                .WithOne(h => h.Site)
                .HasForeignKey(h => h.SiteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
