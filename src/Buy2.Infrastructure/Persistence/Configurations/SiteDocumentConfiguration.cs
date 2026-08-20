using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class SiteDocumentConfiguration : IEntityTypeConfiguration<SiteDocument>
{
    public void Configure(EntityTypeBuilder<SiteDocument> builder)
    {
        builder.Property(d => d.FileName)
           .IsRequired()
           .HasColumnType("nvarchar(100)");
        builder.Property(d => d.FilePath)
            .IsRequired()
            .HasColumnType("nvarchar(max)");
        builder.Property(d => d.UploadedAt)
            .IsRequired();

        builder.HasOne<Site>()
            .WithMany()
            .HasForeignKey(d => d.SiteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}