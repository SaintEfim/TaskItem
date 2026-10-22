using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using TaskApi.Models;
using TaskApi.Persistence;
using Xunit;

namespace TaskApi.IntegrationTests;

public class TaskEndpointsTests : IAsyncLifetime
{
    private const string TestConnectionString =
        "Host=127.0.0.1;Port=55432;Database=taskitem_tests;Username=taskitem;Password=taskitem_dev";

    private readonly WebApplicationFactory<Program> _factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TaskDatabase"] = TestConnectionString
            })));

    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        await db.Database.EnsureCreatedAsync();
        await db.Tasks.ExecuteDeleteAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
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

        var task = await response.Content.ReadFromJsonAsync<TaskItem>();
        Assert.NotNull(task);
        Assert.Equal("Buy milk", task.Title);
        Assert.Equal(Priority.High, task.Priority);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        Assert.True(await db.Tasks.AnyAsync(x => x.Id == task.Id));
    }

    [Fact]
    public async Task GetTasks_ReturnsCreatedTask()
    {
        var created = await _client.PostAsJsonAsync("/tasks", new { title = "Read book" });
        created.EnsureSuccessStatusCode();

        var response = await _client.GetAsync("/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskItem>>();
        Assert.Contains(tasks!, x => x.Title == "Read book" && x.Priority == Priority.Medium);
    }

    [Fact]
    public async Task DeleteTask_RemovesTask()
    {
        var created = await _client.PostAsJsonAsync("/tasks", new { title = "Delete me" });
        created.EnsureSuccessStatusCode();
        var task = await created.Content.ReadFromJsonAsync<TaskItem>();
        Assert.NotNull(task);

        var response = await _client.DeleteAsync($"/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        Assert.False(await db.Tasks.AnyAsync(x => x.Id == task.Id));
    }
}
