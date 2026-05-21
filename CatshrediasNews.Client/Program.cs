using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CatshrediasNews.Client;
using CatshrediasNews.Client.Services;
using CatshrediasNews.Client.Resources;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiMode = builder.Configuration["Api:Mode"];
var apiEndpoints = builder.Configuration.GetSection("Api:Endpoints");
var configuredApiBaseUrl = builder.Configuration["Api:BaseUrl"];
var apiBaseUrl = ResolveApiBaseUrl(apiMode, apiEndpoints, configuredApiBaseUrl, builder.HostEnvironment.BaseAddress);

builder.Services.AddScoped<CultureHttpHandler>();
builder.Services.AddScoped<UnauthorizedLogoutHandler>();
builder.Services.AddScoped(sp =>
{
    var cultureHandler = sp.GetRequiredService<CultureHttpHandler>();
    var logoutHandler = sp.GetRequiredService<UnauthorizedLogoutHandler>();
    cultureHandler.InnerHandler = logoutHandler;
    logoutHandler.InnerHandler = new HttpClientHandler();
    return new HttpClient(cultureHandler) { BaseAddress = new Uri(apiBaseUrl) };
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ArticleService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<ModerationService>();
builder.Services.AddScoped<GigaChadService>();
builder.Services.AddScoped<ArticleHeadingsService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<CultureService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddLocalization();

var host = builder.Build();

var auth    = host.Services.GetRequiredService<AuthService>();
var theme   = host.Services.GetRequiredService<ThemeService>();
var culture = host.Services.GetRequiredService<CultureService>();
await culture.InitAsync();
await auth.InitAsync();
await theme.InitAsync();

await host.RunAsync();

static string ResolveApiBaseUrl(
    string? mode,
    IConfigurationSection endpointsSection,
    string? configuredValue,
    string hostBaseAddress)
{
    var modeKey = mode?.Trim().ToLowerInvariant();
    if (!string.IsNullOrWhiteSpace(modeKey))
    {
        var modeUrl = endpointsSection[modeKey];
        if (!string.IsNullOrWhiteSpace(modeUrl) && Uri.TryCreate(modeUrl, UriKind.Absolute, out var modeAbsolute))
            return EnsureTrailingSlash(modeAbsolute.ToString());
    }

    var trimmed = configuredValue?.Trim();
    if (string.IsNullOrWhiteSpace(trimmed) || trimmed == "/")
        return EnsureTrailingSlash(hostBaseAddress);

    if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute))
        return EnsureTrailingSlash(absolute.ToString());

    if (Uri.TryCreate(new Uri(hostBaseAddress), trimmed, out var relative))
        return EnsureTrailingSlash(relative.ToString());

    return EnsureTrailingSlash(hostBaseAddress);
}

static string EnsureTrailingSlash(string value) =>
    value.EndsWith('/') ? value : value + "/";
