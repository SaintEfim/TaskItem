using Microsoft.EntityFrameworkCore;
using TaskApi.Models;
using TaskApi.Persistence;

namespace TaskApi.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var tasks = endpoints.MapGroup("/tasks");

        tasks.MapPost("", async (
            CreateTaskRequest request,
            TaskDbContext db,
            CancellationToken ct) =>
        {
            TaskItem task;

            try
            {
                task = TaskItem.Create(request.Title, request.Priority);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }

            db.Tasks.Add(task);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/tasks/{task.Id}", task);
        });

        tasks.MapGet("", async (
            TaskDbContext db,
            CancellationToken ct) => Results.Ok(await db.Tasks
            .AsNoTracking()
            .ToListAsync(ct)));

        tasks.MapDelete("/{id:guid}", async (
            Guid id,
            TaskDbContext db,
            CancellationToken ct) =>
        {
            var deleted = await db.Tasks
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(ct);

            return deleted > 0 ? Results.NoContent() : Results.NotFound();
        });
    }
}
