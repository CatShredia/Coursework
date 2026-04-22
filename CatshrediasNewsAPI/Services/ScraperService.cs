using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Services;

public class ScraperService(
    IServiceScopeFactory scopeFactory,
    TagMappingService tagMapping,
    ILogger<ScraperService> logger,
    IHttpClientFactory httpFactory)
{
    // ? ParseSourceAsync : скачивает страницу-каталог, собирает ссылки на статьи и парсит каждую
    // вызывается из RssFetcherService.FetchAllAsync
    public async Task ParseSourceAsync(RssSource source)
    {
        logger.LogInformation("Скрапинг источника: {Name} ({Url})", source.Name, source.Url);

        if (string.IsNullOrWhiteSpace(source.LinkSelector))
        {
            logger.LogWarning("Источник {Name}: не задан LinkSelector, пропускаем.", source.Name);
            return;
        }

        var http = httpFactory.CreateClient("scraper");

        // Загружаем страницу-каталог
        string catalogHtml;
        try { catalogHtml = await http.GetStringAsync(source.Url); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка загрузки каталога {Url}", source.Url);
            return;
        }

        var catalogDoc = new HtmlDocument();
        catalogDoc.LoadHtml(catalogHtml);

        // Собираем ссылки на статьи
        var linkNodes = catalogDoc.DocumentNode.SelectNodes(ToXPath(source.LinkSelector));
        if (linkNodes is null || linkNodes.Count == 0)
        {
            logger.LogWarning("Источник {Name}: ссылки по селектору «{Sel}» не найдены.", source.Name, source.LinkSelector);
            return;
        }

        var baseUri = new Uri(source.Url);
        var links   = linkNodes
            .Select(n => n.GetAttributeValue("href", null) ?? n.SelectSingleNode(".//a")?.GetAttributeValue("href", null))
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => h!.StartsWith("http") ? h : new Uri(baseUri, h).ToString())
            .Distinct()
            .Take(20) // не более 20 статей за один проход
            .ToList();

        logger.LogInformation("Источник {Name}: найдено {Count} ссылок.", source.Name, links.Count);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var status = await db.PublicationStatuses.FirstAsync(s =>
            s.Name == (source.IsTrusted ? "Published" : "PendingReview"));
        var allTags = await db.Tags.ToListAsync();

        int created = 0;
        foreach (var url in links)
        {
            if (await db.Articles.AnyAsync(a => a.SourceUrl == url)) continue;

            var article = await ScrapeArticleAsync(http, source, url, status.Id, allTags, db);
            if (article is not null)
            {
                db.Articles.Add(article);
                await db.SaveChangesAsync();
                created++;
                await Task.Delay(500); // rate limiting
            }
        }

        source.LastFetchedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        logger.LogInformation("Источник {Name}: добавлено {Count} новых статей.", source.Name, created);
    }

    private async Task<Article?> ScrapeArticleAsync(
        HttpClient http, RssSource source, string url,
        int statusId, List<Tag> allTags, AppDbContext db)
    {
        string html;
        try { html = await http.GetStringAsync(url); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось загрузить статью {Url}", url);
            return null;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var title   = Extract(doc, source.TitleSelector)   ?? ExtractMeta(doc, "og:title") ?? "Без заголовка";
        var content = Extract(doc, source.ContentSelector) ?? ExtractMeta(doc, "og:description") ?? "";
        var image   = ExtractImage(doc, source.ImageSelector) ?? ExtractMeta(doc, "og:image");
        var dateStr = Extract(doc, source.DateSelector);
        var date    = TryParseDate(dateStr) ?? DateTime.UtcNow;

        // Определяем теги по содержимому
        var words       = (title + " " + content).ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matchedTags = tagMapping.Map(words).Select(name =>
            allTags.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .Where(t => t is not null).Select(t => t!).ToList();

        var article = new Article
        {
            Title       = title.Trim(),
            Content     = TagMappingService.StripHtml(content),
            ContentHtml = TagMappingService.SanitizeHtml(content),
            ImageUrl    = image,
            SourceUrl   = url,
            PublishedAt = date,
            StatusId    = statusId,
            RssSourceId = source.Id
        };

        // Теги добавим после SaveChanges
        if (matchedTags.Count > 0)
        {
            db.Articles.Add(article);
            await db.SaveChangesAsync();
            db.ArticleTags.AddRange(matchedTags.Select(t => new ArticleTag
            {
                ArticleId = article.Id,
                TagId     = t.Id
            }));
            return null; // уже сохранено
        }

        return article;
    }

    // Конвертирует CSS-селектор в XPath (поддержка обычных и CSS-модульных классов)
    private static string ToXPath(string css)
    {
        var parts = css.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var xpath = string.Join("//", parts.Select(CssPartToXPath));
        return "//" + xpath;
    }

    private static string CssPartToXPath(string part)
    {
        if (part.Contains('>'))
        {
            var sub = part.Split('>').Select(p => p.Trim()).ToArray();
            return string.Join("/", sub.Select(CssPartToXPath));
        }

        var tag  = System.Text.RegularExpressions.Regex.Match(part, @"^[a-zA-Z0-9]*").Value;
        var node = string.IsNullOrEmpty(tag) ? "*" : tag;
        var preds = new List<string>();

        // Все классы (.класс)
        foreach (System.Text.RegularExpressions.Match m in
            System.Text.RegularExpressions.Regex.Matches(part, @"\.([a-zA-Z0-9_-]+)"))
            preds.Add($"contains(@class,'{m.Groups[1].Value}')");

        // id (#id)
        var id = System.Text.RegularExpressions.Regex.Match(part, @"#([a-zA-Z0-9_-]+)").Groups[1].Value;
        if (!string.IsNullOrEmpty(id)) preds.Add($"@id='{id}'");

        return preds.Count > 0 ? $"{node}[{string.Join(" and ", preds)}]" : node;
    }

    private static string? Extract(HtmlDocument doc, string? selector)
    {
        if (string.IsNullOrWhiteSpace(selector)) return null;
        var node = doc.DocumentNode.SelectSingleNode(ToXPath(selector));
        return node is null ? null : HtmlEntity.DeEntitize(node.InnerText).Trim();
    }

    private static string? ExtractImage(HtmlDocument doc, string? selector)
    {
        if (string.IsNullOrWhiteSpace(selector)) return null;
        var node = doc.DocumentNode.SelectSingleNode(ToXPath(selector));
        return node?.GetAttributeValue("src", null) ?? node?.GetAttributeValue("data-src", null);
    }

    private static string? ExtractMeta(HtmlDocument doc, string property)
    {
        var node = doc.DocumentNode.SelectSingleNode($"//meta[@property='{property}']")
                ?? doc.DocumentNode.SelectSingleNode($"//meta[@name='{property}']");
        return node?.GetAttributeValue("content", null);
    }

    private static DateTime? TryParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var formats = new[]
        {
            "dd.MM.yyyy", "dd.MM.yyyy HH:mm", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss",
            "dd MMMM yyyy", "d MMMM yyyy HH:mm"
        };
        foreach (var fmt in formats)
            if (DateTime.TryParseExact(raw.Trim(), fmt,
                System.Globalization.CultureInfo.GetCultureInfo("ru-RU"),
                System.Globalization.DateTimeStyles.None, out var dt))
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        if (DateTime.TryParse(raw, out var fallback))
            return DateTime.SpecifyKind(fallback, DateTimeKind.Utc);

        return null;
    }
}
