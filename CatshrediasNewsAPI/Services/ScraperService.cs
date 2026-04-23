using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.Models;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text.Json;

namespace CatshrediasNewsAPI.Services;

public class ScraperService(
    IServiceScopeFactory scopeFactory,
    TagMappingService tagMapping,
    IHttpClientFactory httpFactory,
    IWebHostEnvironment env)
{
    private static readonly Encoding Win1251 = GetWin1251Encoding();

    private static Encoding GetWin1251Encoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1251);
    }

    // Вспомогательный метод для вывода в консоль
    private static void Log(string level, string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}");
    }

    public async Task ParseSourceAsync(RssSource source)
    {
        Log("INFO", $"=== НАЧАЛО СКРАПИНГА: Источник '{source.Name}' ({source.Url}) ===");

        if (string.IsNullOrWhiteSpace(source.LinkSelector))
        {
            Log("WARN", $"Источник {source.Name}: не задан LinkSelector, пропускаем.");
            return;
        }

        var http = httpFactory.CreateClient("scraper");

        string linkXPath;
        try
        {
            linkXPath = ToXPath(source.LinkSelector);
        }
        catch (Exception ex)
        {
            Log("ERROR", $"Источник {source.Name}: некорректный LinkSelector '{source.LinkSelector}'. {ex.Message}");
            return;
        }

        // 1. Загрузка страницы-каталога
        Log("DEBUG", $"Источник {source.Name}: Загружаю каталог...");
        HtmlDocument catalogDoc;
        try 
        { 
            catalogDoc = await DownloadHtmlDocumentAsync(http, source.Url);
            
            // Сохраняем в wwwroot для проверки глазами
            await SaveHtmlToWwwRootAsync(
                $"catalog_{SanitizeFileName(source.Name)}_{DateTime.Now:yyyyMMdd_HHmmss}.html",
                catalogDoc.DocumentNode.OuterHtml);
            
            Log("DEBUG", $"Источник {source.Name}: Каталог загружен.");
        }
        catch (Exception ex)
        {
            Log("ERROR", $"Ошибка загрузки каталога {source.Url}. Exception: {ex.Message}");
            return;
        }

        // 2. Поиск ссылок
        Log("DEBUG", $"Источник {source.Name}: Ищу ссылки по селектору '{source.LinkSelector}'...");
        var linkNodes = catalogDoc.DocumentNode.SelectNodes(linkXPath);
        
        if (linkNodes is null || linkNodes.Count == 0)
        {
            Log("WARN", $"Источник {source.Name}: Ссылки не найдены.");
            return;
        }

        Log("INFO", $"Источник {source.Name}: Найдено узлов: {linkNodes.Count}");

        // 3. Обработка ссылок
        var baseUri = new Uri(source.Url);
        var links = linkNodes
            .Select(ExtractLinkFromNode)
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => TryResolveAbsoluteUrl(baseUri, h!))
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Cast<string>()
            .Where(link => IsLikelyArticleLink(source.Url, link))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();

        if (links.Count == 0)
        {
            Log("DEBUG", $"Источник {source.Name}: fallback-поиск ссылок в raw HTML...");
            links = ExtractLinksFromRawHtml(catalogDoc.DocumentNode.OuterHtml, source.Url)
                .Where(link => IsLikelyArticleLink(source.Url, link))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToList();
        }

        Log("INFO", $"Источник {source.Name}: Осталось {links.Count} уникальных ссылок.");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var status = await db.PublicationStatuses.FirstAsync(s =>
            s.Name == (source.IsTrusted ? "Published" : "PendingReview"));
        
        var allTags = await db.Tags.ToListAsync();

        int created = 0;
        int skipped = 0;
        int errors = 0;

        // 4. Цикл обработки статей
        for (int i = 0; i < links.Count; i++)
        {
            var url = links[i];
            Log("INFO", $"[{i + 1}/{links.Count}] Обработка: {url}");

            if (await db.Articles.AnyAsync(a => a.SourceUrl == url)) 
            {
                Log("DEBUG", $"Пропуск (уже есть): {url}");
                skipped++;
                continue;
            }

            try 
            {
                var createdNow = await ScrapeAndPersistArticleAsync(http, source, url, status.Id, allTags, db);
                
                if (createdNow)
                {
                    created++;
                }
                else 
                {
                    skipped++;
                }
            }
            catch (Exception ex)
            {
                Log("ERROR", $"Ошибка статьи {url}: {ex.Message}");
                errors++;
            }

            if (i < links.Count - 1) 
            {
                await Task.Delay(500); 
            }
        }

        source.LastFetchedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        
        Log("INFO", $"=== ИТОГ: {source.Name}. Добавлено: {created}, Пропущено: {skipped}, Ошибок: {errors} ===");
    }

    // ? ScrapeAndPersistArticleAsync : загружает страницу статьи, парсит поля и сохраняет в БД
    // вызывается из ParseSourceAsync
    private async Task<bool> ScrapeAndPersistArticleAsync(
        HttpClient http, RssSource source, string url,
        int statusId, List<Tag> allTags, AppDbContext db)
    {
        Log("DEBUG", $">> Парсинг статьи: {url}");

        HtmlDocument doc;
        try 
        { 
            doc = await DownloadHtmlDocumentAsync(http, url);
            
            // Сохраняем в wwwroot
            string fileName = $"article_{Math.Abs(url.GetHashCode())}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            await SaveHtmlToWwwRootAsync(fileName, doc.DocumentNode.OuterHtml, subFolder: "articles");
            
            Log("DEBUG", $"HTML статьи сохранен: {fileName}");
        }
        catch (Exception ex)
        {
            Log("WARN", $"Не удалось загрузить {url}: {ex.Message}");
            return false;
        }

        var titleNode = ExtractNode(doc, source.TitleSelector);
        var contentNode = ExtractBestContentNode(doc, source.ContentSelector);

        var title = HtmlEntity.DeEntitize(titleNode?.InnerText ?? "").Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            title = ExtractMeta(doc, "og:title")?.Trim() ?? "Без заголовка";
        }

        var contentRaw = contentNode?.InnerHtml;
        if (string.IsNullOrWhiteSpace(contentRaw))
        {
            contentRaw = ExtractMeta(doc, "og:description") ?? "";
        }

        var image = ExtractImage(doc, source.ImageSelector) ?? ExtractMeta(doc, "og:image");
        var dateStr = ExtractText(doc, source.DateSelector);
        var date = TryParseDate(dateStr) ?? DateTime.UtcNow;
        var jsonLd = ExtractNewsArticleJsonLd(doc);
        if (jsonLd is not null)
        {
            title = FirstNotEmpty(title, jsonLd.Headline, jsonLd.AlternateName) ?? title;
            contentRaw = FirstNotEmpty(contentRaw, jsonLd.ArticleBody, jsonLd.Description) ?? contentRaw;
            image = FirstNotEmpty(image, jsonLd.Image, jsonLd.ThumbnailUrl);
            if (!string.IsNullOrWhiteSpace(jsonLd.DatePublished))
            {
                date = TryParseDate(jsonLd.DatePublished) ?? date;
            }
        }

        title = HtmlEntity.DeEntitize(title).Trim();
        contentRaw = HtmlEntity.DeEntitize(contentRaw);

        var plainTextContent = TagMappingService.StripHtml(contentRaw);

        if (!LooksLikeArticlePage(url, title, plainTextContent))
        {
            Log("INFO", $"Пропуск не-статьи: {url}");
            return false;
        }

        Log("DEBUG", $"Заголовок: '{title}'");

        var searchableText = $"{title} {plainTextContent}";
        var words = searchableText
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var matchedTags = tagMapping.Map(words).Select(name =>
            allTags.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .Where(t => t is not null).Select(t => t!).ToList();

        Log("INFO", $"Найдено тегов: {matchedTags.Count}");

        var article = new Article
        {
            Title       = title.Trim(),
            Content     = plainTextContent,
            ContentHtml = TagMappingService.SanitizeHtml(contentRaw),
            ImageUrl    = image,
            SourceUrl   = url,
            PublishedAt = date,
            StatusId    = statusId,
            RssSourceId = source.Id
        };

        db.Articles.Add(article);
        await db.SaveChangesAsync();

        if (matchedTags.Count > 0)
        {
            db.ArticleTags.AddRange(matchedTags.Select(t => new ArticleTag
            {
                ArticleId = article.Id,
                TagId     = t.Id
            }));
            await db.SaveChangesAsync();
        }

        Log("INFO", $"Сохранено: '{article.Title}'");
        return true;
    }

    // ? DownloadHtmlDocumentAsync : скачивает HTML, выбирает кодировку и возвращает распарсенный документ
    // вызывается из ParseSourceAsync и ScrapeAndPersistArticleAsync
    private async Task<HtmlDocument> DownloadHtmlDocumentAsync(HttpClient http, string url)
    {
        using var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var headerCharset = response.Content.Headers.ContentType?.CharSet;
        var html = DecodeHtml(bytes, headerCharset);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }

    // ? DecodeHtml : определяет корректную кодировку HTML и декодирует байты без кракозябр
    // вызывается из DownloadHtmlDocumentAsync
    private static string DecodeHtml(byte[] bytes, string? headerCharset)
    {
        // 1) Если байты валидны как UTF-8, это почти всегда правильный вариант для современных сайтов.
        if (IsValidUtf8(bytes))
        {
            var utf8Text = Encoding.UTF8.GetString(bytes);
            if (!LooksLikeMojibake(utf8Text))
            {
                return utf8Text;
            }
        }

        // 2) Уважаем явный charset из заголовка.
        var headerEncoding = GetEncodingOrNull(headerCharset);
        if (headerEncoding is not null)
        {
            var headerText = headerEncoding.GetString(bytes);
            if (!LooksLikeMojibake(headerText))
            {
                return headerText;
            }
        }

        // 3) Ищем charset в meta-теге по ASCII-срезу начала документа.
        var ascii = Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 4096));
        var metaCharset = TryGetMetaCharset(ascii);
        var metaEncoding = GetEncodingOrNull(metaCharset);
        if (metaEncoding is not null)
        {
            var metaText = metaEncoding.GetString(bytes);
            if (!LooksLikeMojibake(metaText))
            {
                return metaText;
            }
        }

        // 4) Fallback для старых русскоязычных сайтов.
        var text1251 = Win1251.GetString(bytes);
        if (!LooksLikeMojibake(text1251))
        {
            return text1251;
        }

        // 5) Последний fallback.
        return Encoding.UTF8.GetString(bytes);
    }

    // ? LooksLikeMojibake : эвристика битой перекодировки (кракозябры)
    // вызывается из DecodeHtml
    private static bool LooksLikeMojibake(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var len = Math.Max(1, text.Length);
        var controlCount = text.Count(c => char.IsControl(c) && c is not '\r' and not '\n' and not '\t');
        var replacementCount = text.Count(c => c == '\uFFFD');

        // Частая сигнатура битого UTF-8 в кириллице (например "п╣п╫я┌").
        var mojibakePairs = Regex.Matches(text, "[пП][\\u2500-\\u257F\\w]").Count;

        return (double)replacementCount / len > 0.01
            || (double)controlCount / len > 0.01
            || (double)mojibakePairs / len > 0.03;
    }

    // ? IsValidUtf8 : проверяет, что набор байтов корректно декодируется как UTF-8
    // вызывается из DecodeHtml
    private static bool IsValidUtf8(byte[] bytes)
    {
        try
        {
            _ = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    // ? TryGetMetaCharset : извлекает charset из meta-тегов HTML
    // вызывается из DecodeHtml
    private static string? TryGetMetaCharset(string htmlStart)
    {
        if (string.IsNullOrWhiteSpace(htmlStart))
        {
            return null;
        }

        var direct = Regex.Match(
            htmlStart,
            "<meta[^>]*charset\\s*=\\s*[\"']?([a-zA-Z0-9_\\-]+)",
            RegexOptions.IgnoreCase);
        if (direct.Success)
        {
            return direct.Groups[1].Value;
        }

        var contentType = Regex.Match(
            htmlStart,
            "<meta[^>]*http-equiv\\s*=\\s*[\"']content-type[\"'][^>]*content\\s*=\\s*[\"'][^\"']*charset=([a-zA-Z0-9_\\-]+)",
            RegexOptions.IgnoreCase);
        return contentType.Success ? contentType.Groups[1].Value : null;
    }

    private static Encoding? GetEncodingOrNull(string? charset)
    {
        if (string.IsNullOrWhiteSpace(charset))
        {
            return null;
        }

        try
        {
            return Encoding.GetEncoding(charset.Trim().Trim('"', '\''));
        }
        catch
        {
            return null;
        }
    }

    // ? SaveHtmlToWwwRootAsync : сохраняет отладочный HTML в wwwroot/debug
    // вызывается из ParseSourceAsync и ScrapeAndPersistArticleAsync
    private async Task SaveHtmlToWwwRootAsync(string fileName, string content, string subFolder = "")
    {
        try
        {
            string debugPath = Path.Combine(env.WebRootPath, "debug");
            if (!string.IsNullOrEmpty(subFolder))
            {
                debugPath = Path.Combine(debugPath, subFolder);
            }

            if (!Directory.Exists(debugPath))
            {
                Directory.CreateDirectory(debugPath);
            }

            string fullPath = Path.Combine(debugPath, fileName);

            // Сохраняем в UTF-8, чтобы браузер корректно отображал файл
            await File.WriteAllTextAsync(fullPath, content, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Log("ERROR", $"Не удалось сохранить HTML в wwwroot: {ex.Message}");
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(name.Where(c => !invalidChars.Contains(c)).ToArray()).Replace(" ", "_");
    }

    // --- Методы парсинга (XPath и прочее) без изменений ---

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

        var tag  = Regex.Match(part, @"^[a-zA-Z0-9]*").Value;
        var node = string.IsNullOrEmpty(tag) ? "*" : tag;
        var preds = new List<string>();

        foreach (Match m in Regex.Matches(part, @"\.([a-zA-Z0-9_-]+)"))
            preds.Add($"contains(@class,'{m.Groups[1].Value}')");

        var id = Regex.Match(part, @"#([a-zA-Z0-9_-]+)").Groups[1].Value;
        if (!string.IsNullOrEmpty(id)) preds.Add($"@id='{id}'");

        return preds.Count > 0 ? $"{node}[{string.Join(" and ", preds)}]" : node;
    }

    private static HtmlNode? ExtractNode(HtmlDocument doc, string? selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            return null;
        }

        try
        {
            return doc.DocumentNode.SelectSingleNode(ToXPath(selector));
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractText(HtmlDocument doc, string? selector)
    {
        var node = ExtractNode(doc, selector);
        return node is null ? null : HtmlEntity.DeEntitize(node.InnerText).Trim();
    }

    private static string? ExtractImage(HtmlDocument doc, string? selector)
    {
        var node = ExtractNode(doc, selector);
        return node?.GetAttributeValue("src", null) ?? node?.GetAttributeValue("data-src", null);
    }

    private static string? ExtractLinkFromNode(HtmlNode node)
    {
        var href = node.GetAttributeValue("href", null);
        if (!string.IsNullOrWhiteSpace(href))
        {
            return href;
        }

        var anchor = node.SelectSingleNode(".//a[@href]");
        return anchor?.GetAttributeValue("href", null);
    }

    private static string? TryResolveAbsoluteUrl(Uri baseUri, string href)
    {
        var trimmed = href.Trim();
        if (trimmed.StartsWith('#')
            || trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            return Uri.TryCreate(trimmed, UriKind.Absolute, out var abs)
                ? abs.ToString()
                : new Uri(baseUri, trimmed).ToString();
        }
        catch
        {
            return null;
        }
    }

    // ? IsLikelyArticleLink : отбрасывает разделы/витрины и оставляет ссылки на статьи
    // вызывается из ParseSourceAsync
    private static bool IsLikelyArticleLink(string sourceUrl, string link)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var sourceUri)
            || !Uri.TryCreate(link, UriKind.Absolute, out var articleUri))
        {
            return false;
        }

        var host = sourceUri.Host.ToLowerInvariant();
        var path = articleUri.AbsolutePath.TrimEnd('/').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(path) || path == "/")
        {
            return false;
        }

        // KP: статья обычно вида /daily/<sectionId>/<articleId>/.
        if (host.Contains("kp.ru"))
        {
            return Regex.IsMatch(path, @"^/daily/\d+(?:\.\d+)?/\d+$", RegexOptions.IgnoreCase);
        }

        // Общие стоп-ветки для медиа-разделов.
        var blockedPrefixes = new[]
        {
            "/photo", "/video", "/radio", "/tv", "/archive", "/tags", "/tag",
            "/authors", "/author", "/themes", "/topic", "/topics", "/special"
        };
        if (blockedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }

    // ? LooksLikeArticlePage : минимальная проверка, что страница действительно является статьей
    // вызывается из ScrapeAndPersistArticleAsync
    private static bool LooksLikeArticlePage(string url, string title, string plainTextContent)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Equals("Без заголовка", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Host.Contains("kp.ru", StringComparison.OrdinalIgnoreCase))
        {
            var path = uri.AbsolutePath.TrimEnd('/');
            if (!Regex.IsMatch(path, @"^/daily/\d+(?:\.\d+)?/\d+$", RegexOptions.IgnoreCase))
            {
                return false;
            }

            var lowerTitle = title.ToLowerInvariant();
            if (lowerTitle is "о кп" or "новое на сайте kp.ru")
            {
                return false;
            }

            // На kp часть контента рендерится клиентом, поэтому допускаем короткий текст,
            // если есть адекватный заголовок и это URL статьи.
            return plainTextContent.Trim().Length >= 20 || title.Length >= 20;
        }

        // У остальных сайтов оставляем более строгий порог.
        if (plainTextContent.Trim().Length < 200)
        {
            return false;
        }

        return true;
    }

    private sealed class JsonLdNewsArticle
    {
        public string? Headline { get; set; }
        public string? AlternateName { get; set; }
        public string? Description { get; set; }
        public string? ArticleBody { get; set; }
        public string? DatePublished { get; set; }
        public string? Image { get; set; }
        public string? ThumbnailUrl { get; set; }
    }

    private static JsonLdNewsArticle? ExtractNewsArticleJsonLd(HtmlDocument doc)
    {
        var scripts = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (scripts is null || scripts.Count == 0)
        {
            return null;
        }

        foreach (var script in scripts)
        {
            var json = script.InnerText?.Trim();
            if (string.IsNullOrWhiteSpace(json))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                if (TryReadNewsArticle(document.RootElement, out var model))
                {
                    return model;
                }
            }
            catch
            {
                // ignore malformed json-ld blocks
            }
        }

        return null;
    }

    private static bool TryReadNewsArticle(JsonElement element, out JsonLdNewsArticle model)
    {
        model = new JsonLdNewsArticle();

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (TryReadNewsArticle(item, out model))
                {
                    return true;
                }
            }

            return false;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!IsNewsArticleType(element))
        {
            if (element.TryGetProperty("@graph", out var graph))
            {
                return TryReadNewsArticle(graph, out model);
            }

            return false;
        }

        model.Headline = ReadJsonString(element, "headline");
        model.AlternateName = ReadJsonString(element, "alternateName");
        model.Description = ReadJsonString(element, "description");
        model.ArticleBody = ReadJsonString(element, "articleBody");
        model.DatePublished = ReadJsonString(element, "datePublished");
        model.Image = ReadJsonStringOrUrl(element, "image");
        model.ThumbnailUrl = ReadJsonString(element, "thumbnailUrl");
        return true;
    }

    private static bool IsNewsArticleType(JsonElement element)
    {
        if (!element.TryGetProperty("@type", out var type))
        {
            return false;
        }

        return type.ValueKind switch
        {
            JsonValueKind.String => type.GetString()?.Contains("Article", StringComparison.OrdinalIgnoreCase) == true,
            JsonValueKind.Array => type.EnumerateArray().Any(t => t.ValueKind == JsonValueKind.String
                && (t.GetString()?.Contains("Article", StringComparison.OrdinalIgnoreCase) == true)),
            _ => false
        };
    }

    private static string? ReadJsonString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string? ReadJsonStringOrUrl(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            return ReadJsonString(value, "url");
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    return item.GetString();
                }

                if (item.ValueKind == JsonValueKind.Object)
                {
                    var url = ReadJsonString(item, "url");
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        return url;
                    }
                }
            }
        }

        return null;
    }

    private static string? FirstNotEmpty(params string?[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    }

    private static HtmlNode? ExtractBestContentNode(HtmlDocument doc, string? sourceSelector)
    {
        var candidates = new List<HtmlNode>();
        var sourceNode = ExtractNode(doc, sourceSelector);
        if (sourceNode is not null)
        {
            candidates.Add(sourceNode);
        }

        var fallbackXPaths = new[]
        {
            "//article",
            "//*[@itemprop='articleBody']",
            "//*[contains(@class,'article') and contains(@class,'text')]",
            "//*[contains(@class,'article__text')]",
            "//*[contains(@class,'article-body')]",
            "//*[contains(@class,'news-text')]",
            "//*[contains(@class,'text-content')]"
        };

        foreach (var xpath in fallbackXPaths)
        {
            var node = doc.DocumentNode.SelectSingleNode(xpath);
            if (node is not null)
            {
                candidates.Add(node);
            }
        }

        return candidates
            .OrderByDescending(n => HtmlEntity.DeEntitize(n.InnerText).Trim().Length)
            .FirstOrDefault();
    }

    // ? ExtractLinksFromRawHtml : вытаскивает ссылки из скриптов/JSON, когда в DOM нет обычных <a>
    // вызывается из ParseSourceAsync
    private static IEnumerable<string> ExtractLinksFromRawHtml(string html, string sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        var baseUri = new Uri(sourceUrl);
        var normalized = html.Replace("\\/", "/");
        var matches = Regex.Matches(
            normalized,
            @"https?://[^\s""'<>]+|/[a-zA-Z0-9_\-./]+",
            RegexOptions.IgnoreCase);

        return matches
            .Select(m => m.Value)
            .Where(v => v.Contains("/daily/", StringComparison.OrdinalIgnoreCase))
            .Select(v => TryResolveAbsoluteUrl(baseUri, v))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Cast<string>();
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
                CultureInfo.GetCultureInfo("ru-RU"),
                DateTimeStyles.None, out var dt))
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        if (DateTime.TryParse(raw, out var fallback))
            return DateTime.SpecifyKind(fallback, DateTimeKind.Utc);

        return null;
    }
}