using TaskApi.Models;

namespace TaskApi.Messaging;

public interface ITaskEvents
{
    Task PublishCompletedAsync(
        TaskItem task);
}
