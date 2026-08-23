using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations
{
    public class SiteConfiguration : IEntityTypeConfiguration<Site>
    {   
        public void Configure(EntityTypeBuilder<Site> builder)
        {
            builder.Property(s => s.SiteName)
                .IsRequired()
            .HasColumnType("nvarchar(100)");
            builder.HasIndex(s => s.SiteName)
                .IsUnique();

            builder.Property(s => s.Latitude)
                .IsRequired()
                .HasPrecision(9, 6);
            builder.Property(s => s.Longitude)
               .IsRequired()
               .HasPrecision(9, 6);
            builder.Property(s => s.MacAddressWhitelistJson)
                .IsRequired(false)
                .HasColumnType("nvarchar(max)");

            builder.Property(s => s.MacAddress)
                .IsRequired(false)
                .HasColumnType("nvarchar(max)");
            builder.Property(s => s.Address)
                .IsRequired()
                .HasColumnType("nvarchar(max)");
            builder.Property(s => s.MapUrl)
                .IsRequired()
                .HasColumnType("nvarchar(max)");
            builder.Property(s => s.PhoneNumber)
                .IsRequired()
                .HasColumnType("nvarchar(100)");
            builder.Property(s => s.Instructions)
                .IsRequired()
                .HasColumnType("nvarchar(max)");
            builder.Property(s => s.MaxCapacity)
                .IsRequired()
                .HasColumnType("int");

            builder.HasMany<Employee>()
                .WithOne(e => e.Site)
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(s => s.Shifts)
                .WithOne()
                .HasForeignKey(s => s.SiteId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(s => s.Region)
                .WithMany(r => r.Sites)
                .HasForeignKey(s => s.RegionId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(s => s.OperationalHours)
                .WithOne(o => o.Site)
                .HasForeignKey(o => o.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(s => s.PreferredEmployees)
                .WithOne(pe => pe.Site)
                .HasForeignKey(pe => pe.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(s => s.Documents)
                .WithOne(d => d.Site)
                .HasForeignKey(d => d.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(s => s.EmployeeSites)
                .WithOne(es => es.Site!)
                .HasForeignKey(es => es.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}