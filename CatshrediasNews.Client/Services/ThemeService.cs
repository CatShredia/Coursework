using Microsoft.JSInterop;

namespace CatshrediasNews.Client.Services;

public class ThemeService(IJSRuntime js)
{
    public bool IsDark { get; private set; }

    public event Action? OnChange;

    public async Task InitAsync()
    {
        var saved = await js.InvokeAsync<string?>("localStorage.getItem", "theme");
        IsDark = saved == "dark";
        await ApplyAsync();
    }

    public async Task ToggleAsync()
    {
        IsDark = !IsDark;
        await js.InvokeVoidAsync("localStorage.setItem", "theme", IsDark ? "dark" : "light");
        await ApplyAsync();
        OnChange?.Invoke();
    }

    private async Task ApplyAsync() =>
        await js.InvokeVoidAsync("eval",
            IsDark
                ? "document.documentElement.setAttribute('data-theme','dark')"
                : "document.documentElement.removeAttribute('data-theme')");
}
