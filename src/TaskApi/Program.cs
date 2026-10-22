using System.Text.Json.Serialization;
using TaskApi.Endpoints;
using TaskApi.Models;
using TaskApi.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<Priority>()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapTaskEndpoints();

app.Run();
