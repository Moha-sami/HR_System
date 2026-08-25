using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class QualificationConfiguration : IEntityTypeConfiguration<Qualification>
{
    public void Configure(EntityTypeBuilder<Qualification> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(q => q.Name)
            .IsUnique();

        builder.Property(q => q.Category)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(q => q.Description)
            .HasMaxLength(500);
    }
}
