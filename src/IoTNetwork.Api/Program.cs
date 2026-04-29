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
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, _, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console()
       .WriteTo.File(
            path: "logs/iotnetwork-api-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            shared: true);
});

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
var allowPrivateNetworks = builder.Configuration.GetValue("Cors:AllowPrivateNetworks", true);
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        static bool IsPrivateOrLocalHost(string host)
        {
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!System.Net.IPAddress.TryParse(host, out var ip))
            {
                return false;
            }

            var bytes = ip.GetAddressBytes();
            if (bytes.Length == 4)
            {
                return bytes[0] == 10
                       || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                       || (bytes[0] == 192 && bytes[1] == 168)
                       || bytes[0] == 127;
            }

            return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.Equals(System.Net.IPAddress.IPv6Loopback);
        }

        policy.SetIsOriginAllowed(origin =>
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (corsOrigins.Length == 0)
            {
                return true;
            }

            if (corsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            return allowPrivateNetworks && IsPrivateOrLocalHost(uri.Host);
        })
        .AllowAnyHeader()
        .AllowAnyMethod();
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

app.UseSerilogRequestLogging();

app.UseCors("Default");

app.UseWhen(
    ctx =>
    {
        var path = ctx.Request.Path.Value;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/hubs/telemetry", StringComparison.OrdinalIgnoreCase);
    },
    branch => branch.UseMiddleware<ApiKeyMiddleware>());

app.MapTelemetryRoutes();
app.MapHub<TelemetryHub>("/hubs/telemetry").RequireCors("Default");

if (app.Environment.IsProduction())
{
    app.MapGet("/", (IConfiguration cfg) =>
    {
        var title = cfg["OpenApi:Title"] ?? "API Red IoT Escolar";
        var envLabel = cfg["OpenApi:EnvironmentLabel"] ?? "Producción";
        var version = cfg["OpenApi:InfoVersion"] ?? "1.0.0";
        var pwaUrl = cfg["Landing:PwaUrl"] ?? "https://iot-mci-app.azurewebsites.net";
        var year = DateTime.UtcNow.Year;
        var utcNow = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");

        var html = $$"""
        <!doctype html>
        <html lang="es">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width,initial-scale=1" />
          <meta name="robots" content="noindex" />
          <title>{{title}}</title>
          <style>
            :root { color-scheme: light dark; }
            * { box-sizing: border-box; }
            body {
              margin:0; font-family: system-ui, -apple-system, Segoe UI, Roboto, sans-serif;
              min-height:100vh; display:flex; align-items:center; justify-content:center;
              background: linear-gradient(135deg,#0ea5e9,#6366f1); color:#fff; padding:24px;
            }
            .card {
              background: rgba(255,255,255,0.08);
              backdrop-filter: blur(12px);
              border: 1px solid rgba(255,255,255,0.18);
              border-radius: 20px; padding: 40px 48px; max-width: 640px; width: 100%;
              box-shadow: 0 20px 60px rgba(0,0,0,0.25);
            }
            h1 { margin:0 0 8px; font-size: clamp(1.5rem, 4vw, 2rem); }
            .badge { display:inline-block; padding:4px 10px; border-radius:999px;
                     background:#10b981; color:#fff; font-size:.8rem; margin-left:8px;
                     vertical-align: middle; }
            p { opacity:.9; line-height:1.5; }
            .grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(200px,1fr));
                    gap:12px; margin-top:24px; }
            .grid a {
              text-decoration:none; color:#fff; padding:12px 16px; border-radius:12px;
              background: rgba(255,255,255,0.12); border:1px solid rgba(255,255,255,0.2);
              transition: transform .15s ease, background .15s ease;
              display:flex; align-items:center; gap:10px;
            }
            .grid a:hover { background: rgba(255,255,255,0.22); transform: translateY(-2px); }
            .footer { margin-top:28px; font-size:.75rem; opacity:.6; }
            code { background:rgba(0,0,0,0.25); padding:2px 6px; border-radius:6px; }
          </style>
        </head>
        <body>
          <div class="card">
            <h1>{{title}} <span class="badge">online</span></h1>
            <p>
              Backend de telemetría IoT para el Instituto Tecnológico de Celaya.<br/>
              Ambiente: <code>{{envLabel}}</code> · Versión: <code>{{version}}</code>
            </p>
            <div class="grid">
              <a href="/swagger">📖 Documentación (Swagger)</a>
              <a href="/health">💓 Health</a>
              <a href="{{pwaUrl}}" target="_blank" rel="noopener">📱 Abrir PWA</a>
            </div>
            <div class="footer">
              © {{year}} ITC — Red IoT escolar.
              Servidor UTC: {{utcNow}}
            </div>
          </div>
        </body>
        </html>
        """;

        return Results.Content(html, "text/html; charset=utf-8");
    }).ExcludeFromDescription();

    app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        utc = DateTime.UtcNow
    })).ExcludeFromDescription();
}

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

try
{
    await app.RunAsync();
}
finally
{
    Log.CloseAndFlush();
}
