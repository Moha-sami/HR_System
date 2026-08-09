using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class ShiftClaimConfiguration : IEntityTypeConfiguration<ShiftClaim>
{
    public void Configure(EntityTypeBuilder<ShiftClaim> builder)
    {
        builder.Property(s => s.Status)
               .IsRequired()
               .HasColumnType("varchar(20)");
        builder.Property(s => s.OvertimeJustification)
               .IsRequired(false)
               .HasColumnType("nvarchar(500)");

        builder.HasOne<ShiftEntity>()
               .WithMany()
               .HasForeignKey(s => s.ShiftId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Employee>()
               .WithMany()
               .HasForeignKey(s => s.EmployeeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
