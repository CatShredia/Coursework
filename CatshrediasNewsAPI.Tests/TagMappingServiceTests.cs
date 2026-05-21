using CatshrediasNewsAPI.Services;
using Microsoft.Extensions.Configuration;

namespace CatshrediasNewsAPI.Tests;

public class TagMappingServiceTests
{
    private static TagMappingService CreateService() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RssFetcher:TagMappingRules:IT:0"] = "python",
                ["RssFetcher:TagMappingRules:IT:1"] = "rust",
                ["RssFetcher:TagMappingRules:Science:0"] = "physics"
            })
            .Build());

    [Fact]
    public void Map_MatchesKeywordsCaseInsensitive()
    {
        var service = CreateService();
        var tags = service.Map(["Python tutorials", "other"]);
        Assert.Contains("IT", tags);
        Assert.DoesNotContain("Science", tags);
    }

    [Fact]
    public void StripHtml_RemovesTags()
    {
        var text = TagMappingService.StripHtml("<p>Hello <b>world</b></p>");
        Assert.Equal("Hello world", text);
    }

    [Fact]
    public void ExtractFirstImage_ReturnsSrc()
    {
        var url = TagMappingService.ExtractFirstImage("<div><img src=\"/img.png\" alt=\"x\" /></div>");
        Assert.Equal("/img.png", url);
    }

    [Fact]
    public void SanitizeHtml_RemovesScriptAndOnClick()
    {
        var html = TagMappingService.SanitizeHtml(
            "<p onclick=\"alert(1)\">Hi</p><script>evil()</script>");
        Assert.DoesNotContain("script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hi", html);
    }
}
