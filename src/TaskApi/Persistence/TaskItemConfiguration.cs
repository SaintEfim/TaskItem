using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskApi.Models;

namespace TaskApi.Persistence;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(
        EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.Property(task => task.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(task => task.IsCompleted)
            .IsRequired();

        builder.Property(task => task.CreatedAt)
            .IsRequired();

        builder.Property(task => task.CompletedAt)
            .IsRequired(false);

        builder.Property(task => task.Priority)
            .HasConversion<string>()
            .HasMaxLength(6)
            .IsRequired();

        // Npgsql maps a uint row version to PostgreSQL's system column xmin.
        builder.Property(task => task.RowVersion)
            .IsRowVersion();
    }
}
