using System.Text;
using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.Hubs;
using CatshrediasNewsAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
const string apiHttpUrl = "http://localhost:5070";
const string apiHttpsUrl = "https://localhost:7240";
var blazorOrigins = new[]
{
    "http://localhost:5110",
    "https://localhost:7255"
};

builder.WebHost.UseUrls(apiHttpsUrl, apiHttpUrl);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
        policy
            .WithOrigins(blazorOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CatshrediasNews API", Version = "v1" });

    // Добавляем поддержку JWT в Swagger UI
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
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient("scraper", c =>
{
    c.Timeout = TimeSpan.FromSeconds(15);
    c.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
    c.DefaultRequestHeaders.Accept.ParseAdd(
        "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    c.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en;q=0.8");
    c.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
    c.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
    c.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
    c.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.GZip
                           | System.Net.DecompressionMethods.Deflate
                           | System.Net.DecompressionMethods.Brotli
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmailService>();
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
builder.Services.AddHostedService(sp => sp.GetRequiredService<RssFetcherService>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Раздаём загруженные файлы из папки uploads рядом с проектом
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath  = "/uploads"
});
app.UseStaticFiles(); // wwwroot
app.UseCors("BlazorClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<CommentsHub>("/hubs/comments");

app.Run();
