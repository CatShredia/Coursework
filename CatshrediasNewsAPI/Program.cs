using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.Extensions;
using CatshrediasNewsAPI.Hubs;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
const string apiHttpUrl = "http://localhost:5070";
const string apiHttpsUrl = "https://localhost:7240";

if (builder.Environment.IsDevelopment())
    builder.WebHost.UseUrls(apiHttpsUrl, apiHttpUrl);

builder.AddRunewsServices();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StartupDatabase");
    const int maxRetries = 10;
    var delay = TimeSpan.FromSeconds(3);
    var migrationsApplied = false;

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            db.Database.Migrate();
            logger.LogInformation("Миграции БД применены успешно.");
            migrationsApplied = true;
            break;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            logger.LogWarning(ex, "Не удалось применить миграции (попытка {Attempt}/{Max}). Повтор через {Delay}s.", attempt, maxRetries, delay.TotalSeconds);
            Thread.Sleep(delay);
        }
    }

    if (migrationsApplied)
    {
        await DatabaseSeedRunner.ApplySeedAsync(db, env, logger);
    }
    else
    {
        logger.LogError("Миграции не применены — seed.sql пропущен.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// CORS до HTTPS-редиректа: иначе OPTIONS (preflight) на http://5070 уходит 307 на https и браузер блокирует запрос.
var corsPolicyName = app.Environment.IsDevelopment() ? "DevelopmentLocal" : "BlazorClient";
app.UseCors(corsPolicyName);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();

    var blockedTestPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "/hub-test.html",
        "/rss-test.html"
    };

    app.Use(async (context, next) =>
    {
        if (blockedTestPages.Contains(context.Request.Path.Value ?? string.Empty))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next();
    });
}

// Раздаём загруженные файлы из папки uploads рядом с проектом
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath  = "/uploads"
});
app.UseStaticFiles(); // wwwroot
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<CommentsHub>("/hubs/comments");
app.MapGet("/health", (IWebHostEnvironment env) =>
    Results.Ok(new { status = "ok", utc = DateTime.UtcNow, environment = env.EnvironmentName }));

app.Run();

public partial class Program { }
