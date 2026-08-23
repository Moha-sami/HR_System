using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class TaskAssignmentConfiguration : IEntityTypeConfiguration<TaskAssignment>
{
    public void Configure(EntityTypeBuilder<TaskAssignment> builder)
    {
        builder.Property(ta => ta.Role)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("nvarchar(50)");

        builder.HasOne(ta => ta.TaskList)
            .WithMany(tl => tl.TaskAssignments)
            .HasForeignKey(ta => ta.TaskListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ta => ta.Employee)
            .WithMany()
            .HasForeignKey(ta => ta.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
