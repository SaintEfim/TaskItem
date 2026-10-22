using System.Text.Json.Serialization;

namespace TaskApi.Models;

public class TaskItem
{
    // For EF Core.
    private TaskItem()
    {
    }

    private TaskItem(
        string title,
        Priority priority)
    {
        Id = Guid.NewGuid();
        Title = title;
        CreatedAt = DateTimeOffset.UtcNow;
        Priority = priority;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = null!;

    public bool IsCompleted { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Priority Priority { get; private set; }

    [JsonIgnore]
    public uint RowVersion { get; private set; }

    public static TaskItem Create(
        string title,
        Priority priority = Priority.Medium)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (title.Length > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(title), "Title must not exceed 200 characters.");
        }

        return !Enum.IsDefined(priority)
            ? throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be Low, Medium or High.")
            : new TaskItem(title, priority);
    }

    public void Completed()
    {
        if (IsCompleted)
        {
            return;
        }

        IsCompleted = true;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
