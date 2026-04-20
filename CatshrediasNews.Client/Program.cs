using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CatshrediasNews.Client;
using CatshrediasNews.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7240/")
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ArticleService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<ModerationService>();
builder.Services.AddScoped<GigaChadService>();

var host = builder.Build();

var auth = host.Services.GetRequiredService<AuthService>();
await auth.InitAsync();

await host.RunAsync();
