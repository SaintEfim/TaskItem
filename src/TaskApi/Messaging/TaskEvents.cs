using System.Text.Json;
using TaskApi.Models;

namespace TaskApi.Messaging;

public sealed class TaskEvents(ILogger<TaskEvents> logger) : ITaskEvents,
    IHostedService
{
    private readonly SemaphoreSlim _publishing = new(1, 1);
    private volatile bool _stopping;

    public Task StartAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task StopAsync(
        CancellationToken cancellationToken)
    {
        _stopping = true;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await _publishing.WaitAsync(timeout.Token);
            _publishing.Release();

            // TODO: close the RabbitMQ channel, then the connection, using timeout.Token.
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("RabbitMQ shutdown was cancelled or reached its 5-second limit.");
        }
    }

    // Call after the completed task has been saved with SaveChangesAsync.
    public async Task PublishCompletedAsync(
        TaskItem task)
    {
        var message = JsonSerializer.Serialize(new
        {
            taskId = task.Id,
            title = task.Title,
            completedAt = task.CompletedAt,
            priority = task.Priority.ToString()
        });

        await _publishing.WaitAsync();
        try
        {
            if (_stopping)
            {
                logger.LogWarning("Application is stopping. Unpublished message: {Message}", message);
                return;
            }

            // TODO: publish to task.events with routing key task.completed.
            logger.LogWarning("RabbitMQ publishing is TODO. Message: {Message}", message);
        }
        finally
        {
            _publishing.Release();
        }
    }
}
