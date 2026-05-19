using CatshrediasNewsAPI.Services;

namespace CatshrediasNewsAPI.Tests;

public class CultureHelperTests
{
    [Theory]
    [InlineData("en", null, "en")]
    [InlineData("EN", "ru-RU", "en")]
    [InlineData("tt", null, "tt")]
    [InlineData("TT", "en-US", "tt")]
    [InlineData(null, "en-US,ru", "en")]
    [InlineData(null, "tt-RU,ru", "tt")]
    [InlineData(null, "ru-RU", "ru")]
    [InlineData("ru", "en-US", "ru")]
    [InlineData(null, null, "ru")]
    public void Resolve_ReturnsExpectedCulture(string? explicitCulture, string? acceptLanguage, string expected)
    {
        var result = CultureHelper.Resolve(explicitCulture, acceptLanguage);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("en", true)]
    [InlineData("EN", true)]
    [InlineData("ru", false)]
    [InlineData("tt", false)]
    public void IsEnglish_Works(string culture, bool expected)
    {
        Assert.Equal(expected, CultureHelper.IsEnglish(culture));
    }

    [Theory]
    [InlineData("tt", true)]
    [InlineData("TT", true)]
    [InlineData("en", false)]
    public void IsTatar_Works(string culture, bool expected)
    {
        Assert.Equal(expected, CultureHelper.IsTatar(culture));
    }
}
