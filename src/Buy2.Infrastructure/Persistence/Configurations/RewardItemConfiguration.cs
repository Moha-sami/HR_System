using Buy2.Application.Features.Employees.BulkOnboard;
using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class RewardItemConfiguration : IEntityTypeConfiguration<RewardItem>
{
    public void Configure(EntityTypeBuilder<RewardItem> builder)
    {
        builder.Property(r => r.RewardName)
            .IsRequired()
            .HasColumnType("nvarchar(100)");
        builder.Property(r => r.CostInPoints)
            .HasColumnType("int");
        builder.Property(r => r.AvailableStock)
            .HasColumnType("int");
        builder.Property(r => r.Description)
            .HasColumnType("nvarchar(max)");
        builder.Property(r => r.MonetaryValue)
            .HasColumnType("decimal(18,2)");
        builder.Property(r => r.BannerImageUrl)
            .HasColumnType("nvarchar(max)");
        builder.Property(r => r.HowToRedeem)
            .HasColumnType("nvarchar(max)");
        builder.Property(r => r.TermsOfUse)
            .HasColumnType("nvarchar(max)");
        builder.Property(r => r.IsActive)
            .HasColumnType("bit")
            .HasDefaultValue(true);


        builder.HasMany(r => r.Vouchers)
            .WithOne(r => r.RewardItem)
            .HasForeignKey(r => r.RewardItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Redemptions)
            .WithOne(r => r.RewardItem)
            .HasForeignKey(r => r.RewardItemId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}