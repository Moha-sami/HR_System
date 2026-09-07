using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;
public class RewardVoucherConfiguration : IEntityTypeConfiguration<RewardVoucher>
{
    public void Configure(EntityTypeBuilder<RewardVoucher> builder)
    {
        builder.Property(r => r.BatchId)
            .IsRequired()
            .HasColumnType("int");
        builder.Property(r => r.Code)
            .IsRequired()
            .HasColumnType("nvarchar(100)");
        builder.Property(r => r.Status)
            .IsRequired();

        builder.HasOne<RewardItem>()
            .WithMany()
            .HasForeignKey(r => r.RewardItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
