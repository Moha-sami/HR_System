using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class ShiftBlockConfiguration : IEntityTypeConfiguration<ShiftBlock>
{
    public void Configure(EntityTypeBuilder<ShiftBlock> builder)
    {
        builder.Property(b => b.StartTime)
            .IsRequired()
            .HasColumnType("time");

        builder.Property(b => b.EndTime)
            .IsRequired()
            .HasColumnType("time");

        builder.HasOne(b => b.ShiftTemplate)
            .WithMany(st => st.ShiftBlocks)
            .HasForeignKey(b => b.ShiftTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.JobRole)
            .WithMany()
            .HasForeignKey(b => b.JobRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Employee)
            .WithMany()
            .HasForeignKey(b => b.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Employee can be assigned to at most one block globally
        // (covers: no duplicate inside same template + no employee in two templates).
        builder.HasIndex(b => b.EmployeeId)
            .IsUnique();

        builder.HasIndex(b => b.ShiftTemplateId);
    }
}
