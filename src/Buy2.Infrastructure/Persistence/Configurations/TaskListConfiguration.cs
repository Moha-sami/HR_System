using Buy2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buy2.Infrastructure.Persistence.Configurations;

public class TaskListConfiguration : IEntityTypeConfiguration<TaskList>
{
    public void Configure(EntityTypeBuilder<TaskList> builder)
    {
        builder.Property(tl => tl.Title)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnType("nvarchar(150)");

        builder.Property(tl => tl.Priority)
            .IsRequired()
            .HasMaxLength(30)
            .HasColumnType("varchar(30)");

        builder.Property(tl => tl.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasColumnType("varchar(30)");

        builder.HasOne(tl => tl.CreatedBy)
            .WithMany()
            .HasForeignKey(tl => tl.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(tl => tl.Organization)
            .WithMany()
            .HasForeignKey(tl => tl.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
