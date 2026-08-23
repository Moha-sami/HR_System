using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnType("nvarchar(150)");

        builder.Property(o => o.LogoUrl)
            .HasMaxLength(500)
            .HasColumnType("varchar(500)");

        builder.Property(o => o.Timezone)
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.Property(o => o.Currency)
            .HasMaxLength(10)
            .HasColumnType("varchar(10)");
    }
}
