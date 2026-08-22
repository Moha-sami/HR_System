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
                .WithOne()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany<ShiftEntity>()
                .WithOne()
                .HasForeignKey(s => s.SiteId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Region>()
                .WithMany()
                .HasForeignKey(s => s.RegionId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany<SiteOperationalHour>()
                .WithOne()
                .HasForeignKey(s => s.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany<SitePreferredEmployee>()
                .WithOne()
                .HasForeignKey(s => s.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany<SiteDocument>()
                .WithOne()
                .HasForeignKey(s => s.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany<EmployeeSite>()
                .WithOne()
                .HasForeignKey(s => s.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}