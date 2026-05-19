namespace CatshrediasNewsAPI.Services;

public static class CultureHelper
{
    public const string English = "en";
    public const string Russian = "ru";
    public const string Tatar = "tt";

    public static string Resolve(string? explicitCulture, string? acceptLanguageHeader)
    {
        if (!string.IsNullOrWhiteSpace(explicitCulture))
        {
            if (IsEnglish(explicitCulture))
                return English;
            if (IsTatar(explicitCulture))
                return Tatar;
            return Russian;
        }

        if (!string.IsNullOrWhiteSpace(acceptLanguageHeader))
        {
            var first = acceptLanguageHeader.Split(',')[0].Trim();
            if (first.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return English;
            if (first.StartsWith("tt", StringComparison.OrdinalIgnoreCase))
                return Tatar;
        }

        return Russian;
    }

    public static bool IsEnglish(string? culture) =>
        string.Equals(culture, English, StringComparison.OrdinalIgnoreCase);

    public static bool IsTatar(string? culture) =>
        string.Equals(culture, Tatar, StringComparison.OrdinalIgnoreCase);
}
