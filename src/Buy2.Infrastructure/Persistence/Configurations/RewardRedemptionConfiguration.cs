using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations
{
    public class RewardRedemptionConfiguration : IEntityTypeConfiguration<RewardRedemption>
    {
        public void Configure(EntityTypeBuilder<RewardRedemption> builder)
        {
            builder.Property(r => r.VoucherCode)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("varchar(100)");
            builder.HasIndex(r => r.VoucherCode)
                .IsUnique();
            builder.Property(r => r.RedeemedAt)
                .IsRequired()
                .HasColumnType("datetimeoffset");

            builder.HasOne(r => r.Employee)
                .WithMany()
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.RewardItem)
                .WithMany(i => i.Redemptions)
                .HasForeignKey(r => r.RewardItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.RewardVoucher)
                .WithMany()
                .HasForeignKey(r => r.RewardVoucherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.PointsTransaction)
                .WithMany()
                .HasForeignKey(r => r.PointTransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
