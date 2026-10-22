using TaskApi.Models;

namespace TaskApi.Endpoints;

public sealed class CreateTaskRequest
{
    public string Title { get; init; } = string.Empty;

    public Priority Priority { get; init; } = Priority.Medium;
}
