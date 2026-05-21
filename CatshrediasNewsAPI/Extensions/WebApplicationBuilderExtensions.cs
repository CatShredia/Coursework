using System.Text;
using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

namespace CatshrediasNewsAPI.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static void AddRunewsServices(this WebApplicationBuilder builder)
    {
        var isTesting = builder.Environment.IsEnvironment("Testing");

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        var configuredAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var corsMode = builder.Configuration["Cors:Mode"]?.Trim().ToLowerInvariant();
        var modeOrigins = !string.IsNullOrWhiteSpace(corsMode)
            ? builder.Configuration.GetSection($"Cors:Profiles:{corsMode}").Get<string[]>() ?? []
            : [];
        var blazorOrigins = modeOrigins.Length > 0 ? modeOrigins : configuredAllowedOrigins;

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("BlazorClient", policy =>
            {
                if (blazorOrigins.Length > 0)
                    policy.WithOrigins(blazorOrigins);
                else
                    policy.SetIsOriginAllowed(_ => false);

                policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            });

            options.AddPolicy("DevelopmentLocal", policy =>
                policy
                    .SetIsOriginAllowed(origin =>
                    {
                        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
                            return false;

                        var isLocal = originUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                            || originUri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);

                        return isLocal || blazorOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "CatshrediasNews API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Введите только сам JWT-токен (без слова Bearer)"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    []
                }
            });
        });

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userIdRaw = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var tokenPwdVersion = context.Principal?.FindFirstValue(AuthService.PasswordVersionClaim);
                        if (!int.TryParse(userIdRaw, out var userId) || string.IsNullOrWhiteSpace(tokenPwdVersion))
                        {
                            context.Fail("Invalid token claims.");
                            return;
                        }

                        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var user = await db.Users
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(u => u.Id == userId);

                        if (user is null || user.DeletedAt is not null || user.IsBlocked)
                        {
                            context.Fail("User not active.");
                            return;
                        }

                        var currentPwdVersion = AuthService.ComputePasswordVersion(user.PasswordHash);
                        if (!string.Equals(tokenPwdVersion, currentPwdVersion, StringComparison.Ordinal))
                            context.Fail("Session expired.");
                    }
                };
            });

        builder.Services.AddAuthorization();
        builder.Services.AddSignalR();
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddHttpClient();
        builder.Services.AddHttpClient("scraper", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            c.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            c.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en;q=0.8");
            c.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip
                                   | System.Net.DecompressionMethods.Deflate
                                   | System.Net.DecompressionMethods.Brotli
        });

        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<ArticleService>();
        builder.Services.AddScoped<TagService>();
        builder.Services.AddScoped<ModerationService>();
        builder.Services.AddScoped<RssSourceService>();
        builder.Services.AddScoped<CommentService>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddSingleton<TagMappingService>();
        builder.Services.AddScoped<IGigaChatService, GigaChatService>();
        builder.Services.AddSingleton<RssParserService>();
        builder.Services.AddSingleton<ScraperService>();
        builder.Services.AddSingleton<RssFetcherService>();

        if (!isTesting)
            builder.Services.AddHostedService(sp => sp.GetRequiredService<RssFetcherService>());
    }
}
