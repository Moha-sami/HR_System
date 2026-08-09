using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class DisciplinaryViolationConfiguration : IEntityTypeConfiguration<DisciplinaryViolation>
{
    public void Configure(EntityTypeBuilder<DisciplinaryViolation> builder)
    {
        builder.Property(d => d.Severity)
            .IsRequired()
            .HasColumnType("varchar(20)");
        builder.Property(d => d.Description)
            .IsRequired()
            .HasColumnType("nvarchar(1000)");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}