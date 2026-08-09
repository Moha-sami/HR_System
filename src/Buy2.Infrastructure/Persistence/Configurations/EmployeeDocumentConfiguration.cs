using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations
{
    public class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
    {
        public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
        {
            builder.Property(ed => ed.Category)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");
            builder.Property(ed => ed.StorageUrl)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("varchar(500)");
            builder.HasOne(ed => ed.Employee)
                .WithMany()
                .HasForeignKey(ed => ed.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
