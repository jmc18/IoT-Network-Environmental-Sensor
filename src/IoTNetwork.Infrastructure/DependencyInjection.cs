using IoTNetwork.Core.Abstractions.Persistence;
using IoTNetwork.Core.Abstractions.Repositories;
using IoTNetwork.Infrastructure.Persistence;
using IoTNetwork.Infrastructure.Persistence.Repositories;
using IoTNetwork.Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IoTNetwork.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<IoTNetworkDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });
        });

        services.AddScoped<ITelemetryReadingRepository, TelemetryReadingRepository>();
        services.AddScoped<INodeDataDayRepository, NodeDataDayRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DevelopmentDataSeeder>();

        return services;
    }
}
