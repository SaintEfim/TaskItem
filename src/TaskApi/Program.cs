using System.Text.Json.Serialization;
using Sieve.Models;
using Sieve.Services;
using TaskApi.Endpoints;
using TaskApi.Messaging;
using TaskApi.Models;
using TaskApi.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddSingleton<TaskEvents>();
builder.Services.AddHostedService(services => services.GetRequiredService<TaskEvents>());
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(5));
builder.Services.AddProblemDetails();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<Priority>()));

builder.Services.Configure<SieveOptions>(options =>
{
    options.DefaultPageSize = 10;
    options.MaxPageSize = 100;
});

builder.Services.AddScoped<ISieveProcessor, SieveProcessor>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapTaskEndpoints();

app.Run();

public partial class Program
{
}
