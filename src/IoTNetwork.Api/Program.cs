using IoTNetwork.Api.Endpoints;
using IoTNetwork.Api.Mappings;
using IoTNetwork.Api.Middleware;
using IoTNetwork.Api.Realtime;
using IoTNetwork.Infrastructure;
using IoTNetwork.Infrastructure.Persistence;
using IoTNetwork.Infrastructure.Persistence.Seeders;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

MappingConfig.RegisterMappings();
builder.Services.AddMapster();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
var openApiTitle = builder.Configuration["OpenApi:Title"] ?? "API Red IoT Escolar";
var openApiDescription = builder.Configuration["OpenApi:Description"] ?? "";
var documentName = builder.Configuration["OpenApi:DocumentName"] ?? "v1";
var infoVersion = builder.Configuration["OpenApi:InfoVersion"] ?? "1.0.0";
var envLabel = builder.Configuration["OpenApi:EnvironmentLabel"];

builder.Services.AddSwaggerGen(options =>
{
    var titleWithEnv = string.IsNullOrWhiteSpace(envLabel)
        ? openApiTitle
        : $"{openApiTitle} ({envLabel})";

    options.SwaggerDoc(documentName, new OpenApiInfo
    {
        Title = titleWithEnv,
        Version = infoVersion,
        Description = openApiDescription,
        Contact = new OpenApiContact
        {
            Name = "Instituto Tecnológico de Celaya",
            Url = new Uri("https://www.itcelaya.edu.mx/")
        }
    });
});

var corsOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 ?? Array.Empty<string>();
var allowAzureWebsites = builder.Configuration.GetValue("Cors:AllowAzureWebsites", true);
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (corsOrigins.Length > 0 || allowAzureWebsites)
        {
            policy.SetIsOriginAllowed(origin =>
                {
                    if (corsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)) return true;
                    if (!allowAzureWebsites) return false;
                    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
                    return uri.Host.EndsWith(".azurewebsites.net", StringComparison.OrdinalIgnoreCase);
                })
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint($"/swagger/{documentName}/swagger.json", $"{openApiTitle} {infoVersion}");
    options.DocumentTitle = string.IsNullOrWhiteSpace(envLabel)
        ? $"{openApiTitle} — Swagger"
        : $"{openApiTitle} ({envLabel}) — Swagger";
});

app.UseCors("Default");

app.MapWhen(
    ctx => ctx.Request.Path.Value?.StartsWith("/api/ingest", StringComparison.OrdinalIgnoreCase) == true,
    branch => branch.UseMiddleware<ApiKeyMiddleware>());

app.MapTelemetryRoutes();
app.MapHub<TelemetryHub>("/hubs/telemetry").RequireCors("Default");

// En producción las migraciones se aplican manualmente (dotnet ef database update
// o contra la BD ya aprovisionada). En Development se aplican automáticamente
// para acelerar el loop. Controlable vía Database:RunMigrationsOnStart.
var runMigrationsOnStart = app.Configuration.GetValue(
    "Database:RunMigrationsOnStart",
    app.Environment.IsDevelopment());

if (runMigrationsOnStart)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IoTNetworkDbContext>();
        await db.Database.MigrateAsync();

        if (app.Environment.IsDevelopment()
            && app.Configuration.GetValue("Seed:EnableDevData", false))
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();
            await seeder.SeedAsync(CancellationToken.None);
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        logger.LogError(ex, "Database migration failed. Ensure PostgreSQL is reachable and connection string is valid.");
        throw;
    }
}

await app.RunAsync();
