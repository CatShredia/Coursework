using HtmlAgilityPack;

namespace CatshrediasNewsAPI.Services;

public class TagMappingService(IConfiguration config)
{
    private readonly Dictionary<string, List<string>> _rules = config
        .GetSection("RssFetcher:TagMappingRules")
        .Get<Dictionary<string, List<string>>>() ?? [];

    // ? MapAsync : сопоставляет список RSS-категорий с именами глобальных тегов из БД
    // вызывается из RssParserService.ParseItemsAsync
    public List<string> Map(IEnumerable<string> rssCategories)
    {
        var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var categories = rssCategories
            .Select(c => c.ToLowerInvariant())
            .ToList();

        foreach (var (tagName, keywords) in _rules)
            if (categories.Any(c => keywords.Any(k => c.Contains(k))))
                matched.Add(tagName);

        return [.. matched];
    }

    // ? StripHtml : удаляет HTML-теги из строки, оставляя только текст
    // вызывается из RssParserService.ParseItemsAsync
    public static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc.DocumentNode.InnerText.Trim();
    }
}
