using IoTNetwork.Api.Endpoints;
using IoTNetwork.Api.Mappings;
using IoTNetwork.Api.Middleware;
using IoTNetwork.Infrastructure;
using IoTNetwork.Infrastructure.Persistence;
using IoTNetwork.Infrastructure.Persistence.Seeders;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

MappingConfig.RegisterMappings();
builder.Services.AddMapster();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

var corsOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Default");

app.MapWhen(
    ctx => ctx.Request.Path.Value?.StartsWith("/api/ingest", StringComparison.OrdinalIgnoreCase) == true,
    branch => branch.UseMiddleware<ApiKeyMiddleware>());

app.MapTelemetryRoutes();

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IoTNetworkDbContext>();
    db.Database.Migrate();

    if (app.Environment.IsDevelopment()
        && app.Configuration.GetValue("Seed:EnableDevData", false))
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();
        seeder.SeedAsync(CancellationToken.None).GetAwaiter().GetResult();
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogError(ex, "Database migration failed. Ensure PostgreSQL is reachable and connection string is valid.");
    throw;
}

app.Run();
