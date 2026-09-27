using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class RequestAttachmentConfiguration : IEntityTypeConfiguration<RequestAttachment>
{
    public void Configure(EntityTypeBuilder<RequestAttachment> builder)
    {
        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnType("nvarchar(255)");

        builder.Property(a => a.StorageUrl)
            .IsRequired()
            .HasMaxLength(1000)
            .HasColumnType("nvarchar(1000)");

        builder.Property(a => a.ContentType)
            .HasMaxLength(100)
            .HasColumnType("varchar(100)");

        builder.HasOne(a => a.Request)
            .WithMany(r => r.Attachments)
            .HasForeignKey(a => a.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
