using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes =
    [
        "application/octet-stream",
        "application/wasm",
        "application/javascript",
        "text/css",
        "text/html",
        "image/svg+xml",
        "application/json",
        "application/manifest+json"
    ];
});

var app = builder.Build();

app.UseResponseCompression();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseBlazorFrameworkFiles();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.Name;
        var headers = ctx.Context.Response.Headers;

        if (string.Equals(path, "service-worker.js", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(path, "firebase-messaging-sw.js", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".webmanifest", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate";
            headers[HeaderNames.Pragma] = "no-cache";
            headers[HeaderNames.Expires] = "0";
        }
        else if (ctx.Context.Request.Path.StartsWithSegments("/_framework"))
        {
            headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
        }

        if (string.Equals(path, "firebase-messaging-sw.js", StringComparison.OrdinalIgnoreCase))
        {
            headers["Service-Worker-Allowed"] = "/";
        }
    }
});

app.MapFallbackToFile("index.html");

app.Run();
