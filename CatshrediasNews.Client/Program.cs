using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CatshrediasNews.Client;
using CatshrediasNews.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7240/";
if (!apiBaseUrl.EndsWith('/'))
    apiBaseUrl += "/";

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
