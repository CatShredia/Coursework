using System.Globalization;
using Microsoft.JSInterop;

namespace CatshrediasNews.Client.Services;

public class CultureService(IJSRuntime js, CultureHttpHandler cultureHttp)
{
    private readonly CultureHttpHandler _cultureHttp = cultureHttp;

    public const string DefaultCulture = "ru";
    public const string EnglishCulture = "en";
    public const string TatarCulture = "tt";

    public static readonly string[] SupportedCultures = [DefaultCulture, EnglishCulture, TatarCulture];

    public string CurrentCulture { get; private set; } = DefaultCulture;

    public event Action? OnChange;

    public bool IsEnglish => CurrentCulture == EnglishCulture;
    public bool IsTatar => CurrentCulture == TatarCulture;
    public bool IsRussian => CurrentCulture == DefaultCulture;

    public async Task InitAsync()
    {
        var saved = await js.InvokeAsync<string?>("localStorage.getItem", "culture");
        Apply(Normalize(string.IsNullOrWhiteSpace(saved) ? DefaultCulture : saved));
        await SetDocumentLangAsync();
    }

    public async Task SetCultureAsync(string culture)
    {
        culture = Normalize(culture);
        if (culture == CurrentCulture) return;

        CurrentCulture = culture;
        await js.InvokeVoidAsync("localStorage.setItem", "culture", culture);
        Apply(culture);
        await SetDocumentLangAsync();
        OnChange?.Invoke();
        await js.InvokeVoidAsync("location.reload");
    }

    public async Task CycleCultureAsync()
    {
        var index = Array.IndexOf(SupportedCultures, CurrentCulture);
        if (index < 0) index = 0;
        var next = SupportedCultures[(index + 1) % SupportedCultures.Length];
        await SetCultureAsync(next);
    }

    public string NextCultureLabelKey() => CurrentCulture switch
    {
        EnglishCulture => "Lang_Tt",
        TatarCulture   => "Lang_Ru",
        _              => "Lang_En"
    };

    public static string Normalize(string culture) =>
        culture switch
        {
            EnglishCulture => EnglishCulture,
            TatarCulture   => TatarCulture,
            _              => DefaultCulture
        };

    private void Apply(string culture)
    {
        CurrentCulture = culture;
        _cultureHttp.SetCulture(culture);
        var ci = CultureInfo.GetCultureInfo(culture);
        CultureInfo.DefaultThreadCurrentCulture = ci;
        CultureInfo.DefaultThreadCurrentUICulture = ci;
    }

    private async Task SetDocumentLangAsync() =>
        await js.InvokeVoidAsync("eval", $"document.documentElement.lang = '{CurrentCulture}'");
}
