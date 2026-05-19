namespace CatshrediasNewsAPI.Services;

public static class CultureHelper
{
    public const string English = "en";
    public const string Russian = "ru";

    public static string Resolve(string? explicitCulture, string? acceptLanguageHeader)
    {
        if (IsEnglish(explicitCulture))
            return English;

        if (!string.IsNullOrWhiteSpace(acceptLanguageHeader))
        {
            var first = acceptLanguageHeader.Split(',')[0].Trim();
            if (first.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return English;
        }

        return Russian;
    }

    public static bool IsEnglish(string? culture) =>
        string.Equals(culture, English, StringComparison.OrdinalIgnoreCase);
}
