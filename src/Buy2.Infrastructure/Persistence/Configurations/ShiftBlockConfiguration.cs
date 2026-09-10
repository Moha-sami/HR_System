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
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Employee assignment on template blocks is a reusable snapshot value:
        // the same employee may appear in many blocks across many templates
        // (e.g. saved daily canvases). The index exists for query performance only.
        builder.HasIndex(b => b.EmployeeId);

        builder.HasIndex(b => b.ShiftTemplateId);
    }
}
