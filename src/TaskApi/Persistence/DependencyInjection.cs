using Microsoft.EntityFrameworkCore;

namespace TaskApi.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TaskDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'TaskDatabase' is not configured.");
        }

        services.AddDbContext<TaskDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
