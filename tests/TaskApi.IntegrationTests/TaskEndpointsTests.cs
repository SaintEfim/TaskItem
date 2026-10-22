using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskApi.Models;
using TaskApi.Persistence;
using Xunit;

namespace TaskApi.IntegrationTests;

public class TaskEndpointsTests : IAsyncLifetime
{
    private readonly string _databaseName = $"taskitem_tests_{Guid.NewGuid():N}";
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _client = null!;

    public TaskEndpointsTests()
    {
        var connectionString =
            $"Host=127.0.0.1;Port=55432;Database={_databaseName};Username=taskitem;Password=taskitem_dev";

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<TaskDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<TaskDbContext>>();
                services.AddDbContext<TaskDbContext>(options => options.UseNpgsql(connectionString));
            }));
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        Assert.Equal(_databaseName, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        try
        {
            if (_client is not null)
            {
                await using var scope = _factory.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
                Assert.Equal(_databaseName, db.Database.GetDbConnection().Database);
                await db.Database.EnsureDeletedAsync();
            }
        }
        finally
        {
            await _factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreateTask_Returns201AndSavesTask()
    {
        var response = await _client.PostAsJsonAsync("/tasks", new
        {
            title = "Buy milk",
            priority = "High"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = task.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal("Buy milk", task.GetProperty("title").GetString());
        Assert.Equal("High", task.GetProperty("priority").GetString());
        Assert.False(task.GetProperty("isCompleted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, task.GetProperty("completedAt").ValueKind);
        Assert.False(task.TryGetProperty("rowVersion", out _));

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        var saved = await db.Tasks.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("Buy milk", saved.Title);
        Assert.Equal(Priority.High, saved.Priority);
        Assert.False(saved.IsCompleted);
        Assert.Null(saved.CompletedAt);
    }

    [Fact]
    public async Task GetTasks_ReturnsCreatedTask()
    {
        var created = await _client.PostAsJsonAsync("/tasks", new { title = "Read book" });
        created.EnsureSuccessStatusCode();
        var task = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = task.GetProperty("id").GetGuid();

        var response = await _client.GetAsync("/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tasks = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(tasks.EnumerateArray(), x =>
            x.GetProperty("id").GetGuid() == id
            && x.GetProperty("title").GetString() == "Read book"
            && x.GetProperty("priority").GetString() == "Medium");
    }

    [Fact]
    public async Task DeleteTask_RemovesTask()
    {
        var created = await _client.PostAsJsonAsync("/tasks", new { title = "Delete me" });
        created.EnsureSuccessStatusCode();
        var task = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = task.GetProperty("id").GetGuid();

        var response = await _client.DeleteAsync($"/tasks/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        Assert.False(await db.Tasks.AnyAsync(x => x.Id == id));
    }

    // TODO: test create -> complete -> publish when the completion endpoint and publisher are implemented.
}
