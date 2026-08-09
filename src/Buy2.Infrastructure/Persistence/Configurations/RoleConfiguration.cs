using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.Property(r => r.Name)
                .HasColumnName("RoleName")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");
            builder.HasIndex(r => r.Name)
                .IsUnique();
            builder.Property(r => r.PermissionsJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");
        }
    }
}
