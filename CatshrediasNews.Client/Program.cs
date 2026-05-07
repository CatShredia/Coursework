using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CatshrediasNews.Client;
using CatshrediasNews.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var configuredApiBaseUrl = builder.Configuration["Api:BaseUrl"];
var apiBaseUrl = ResolveApiBaseUrl(configuredApiBaseUrl, builder.HostEnvironment.BaseAddress);

builder.Services.AddScoped<UnauthorizedLogoutHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<UnauthorizedLogoutHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler)
{
    BaseAddress = new Uri(apiBaseUrl)
};
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ArticleService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<ModerationService>();
builder.Services.AddScoped<GigaChadService>();
builder.Services.AddScoped<ArticleHeadingsService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<CommentService>();

var host = builder.Build();

var auth  = host.Services.GetRequiredService<AuthService>();
var theme = host.Services.GetRequiredService<ThemeService>();
await auth.InitAsync();
await theme.InitAsync();

await host.RunAsync();

static string ResolveApiBaseUrl(string? configuredValue, string hostBaseAddress)
{
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
