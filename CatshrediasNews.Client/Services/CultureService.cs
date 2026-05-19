using System.Globalization;
using Microsoft.JSInterop;

namespace CatshrediasNews.Client.Services;

public class CultureService(IJSRuntime js, CultureHttpHandler cultureHttp)
{
    private readonly CultureHttpHandler _cultureHttp = cultureHttp;
    public const string DefaultCulture = "ru";
    public const string EnglishCulture = "en";

    public string CurrentCulture { get; private set; } = DefaultCulture;

    public event Action? OnChange;

    public bool IsEnglish => CurrentCulture == EnglishCulture;

    public async Task InitAsync()
    {
        var saved = await js.InvokeAsync<string?>("localStorage.getItem", "culture");
        Apply(string.IsNullOrWhiteSpace(saved) ? DefaultCulture : saved);
        await SetDocumentLangAsync();
    }

    public async Task SetCultureAsync(string culture)
    {
        culture = culture == EnglishCulture ? EnglishCulture : DefaultCulture;
        if (culture == CurrentCulture) return;

        CurrentCulture = culture;
        await js.InvokeVoidAsync("localStorage.setItem", "culture", culture);
        Apply(culture);
        await SetDocumentLangAsync();
        OnChange?.Invoke();
        await js.InvokeVoidAsync("location.reload");
    }

    public async Task ToggleCultureAsync() =>
        await SetCultureAsync(IsEnglish ? DefaultCulture : EnglishCulture);

    private void Apply(string culture)
    {
        CurrentCulture = culture;
        _cultureHttp.SetCulture(culture);
        var ci = new CultureInfo(culture);
        CultureInfo.DefaultThreadCurrentCulture = ci;
        CultureInfo.DefaultThreadCurrentUICulture = ci;
    }

    private async Task SetDocumentLangAsync() =>
        await js.InvokeVoidAsync("eval",
            $"document.documentElement.lang = '{(IsEnglish ? "en" : "ru")}'");
}
