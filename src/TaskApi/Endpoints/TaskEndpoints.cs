using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
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
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            TaskDbContext db,
            ISieveProcessor sieve,
            CancellationToken ct) =>
        {
            var model = new SieveModel
            {
                Page = page is > 0 ? page.Value : 1,
                PageSize = pageSize is > 0 ? pageSize.Value : 10
            };

            var query = db.Tasks
                .AsNoTracking()
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id);

            var result = await sieve.Apply(model, query)
                .ToListAsync(ct);

            return Results.Ok(result);
        });

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
