using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CatshrediasNewsAPI.Services;

public class ScraperService(
    IServiceScopeFactory scopeFactory,
    TagMappingService tagMapping,
    IHttpClientFactory httpFactory,
    IWebHostEnvironment env)
{
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

        // 1. Загрузка страницы-каталога с принудительной кодировкой
        Log("DEBUG", $"Источник {source.Name}: Загружаю каталог...");
        string catalogHtml;
        try 
        { 
            catalogHtml = await DownloadHtmlWithEncodingAsync(http, source.Url);
            
            // Сохраняем в wwwroot для проверки глазами
            await SaveHtmlToWwwRootAsync($"catalog_{SanitizeFileName(source.Name)}_{DateTime.Now:yyyyMMdd_HHmmss}.html", catalogHtml);
            
            Log("DEBUG", $"Источник {source.Name}: Каталог загружен. Размер: {catalogHtml.Length} байт");
        }
        catch (Exception ex)
        {
            Log("ERROR", $"Ошибка загрузки каталога {source.Url}. Exception: {ex.Message}");
            return;
        }

        var catalogDoc = new HtmlDocument();
        catalogDoc.LoadHtml(catalogHtml);

        // 2. Поиск ссылок
        Log("DEBUG", $"Источник {source.Name}: Ищу ссылки по селектору '{source.LinkSelector}'...");
        var linkNodes = catalogDoc.DocumentNode.SelectNodes(ToXPath(source.LinkSelector));
        
        if (linkNodes is null || linkNodes.Count == 0)
        {
            Log("WARN", $"Источник {source.Name}: Ссылки не найдены.");
            return;
        }

        Log("INFO", $"Источник {source.Name}: Найдено узлов: {linkNodes.Count}");

        // 3. Обработка ссылок
        var baseUri = new Uri(source.Url);
        var links = linkNodes
            .Select(n => 
            {
                var href = n.GetAttributeValue("href", null);
                if (string.IsNullOrEmpty(href))
                {
                    var anchor = n.SelectSingleNode(".//a");
                    href = anchor?.GetAttributeValue("href", null);
                }
                return href;
            })
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => 
            {
                try 
                {
                    return h!.StartsWith("http") ? h : new Uri(baseUri, h).ToString();
                }
                catch (Exception)
                {
                    return null;
                }
            })
            .Where(h => h != null)
            .Distinct()
            .Take(20)
            .ToList();

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
                var article = await ScrapeArticleAsync(http, source, url, status.Id, allTags, db);
                
                if (article is not null)
                {
                    db.Articles.Add(article);
                    await db.SaveChangesAsync();
                    Log("INFO", $"Сохранено: '{article.Title}'");
                    created++;
                }
                else 
                {
                    Log("INFO", "Сохранено (с тегами).");
                    created++;
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

    private async Task<Article?> ScrapeArticleAsync(
        HttpClient http, RssSource source, string url,
        int statusId, List<Tag> allTags, AppDbContext db)
    {
        Log("DEBUG", $">> Парсинг статьи: {url}");

        string html;
        try 
        { 
            // Используем тот же метод с принудительной кодировкой
            html = await DownloadHtmlWithEncodingAsync(http, url);
            
            // Сохраняем в wwwroot
            string fileName = $"article_{Math.Abs(url.GetHashCode())}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            await SaveHtmlToWwwRootAsync(fileName, html, subFolder: "articles");
            
            Log("DEBUG", $"HTML статьи сохранен: {fileName}");
        }
        catch (Exception ex)
        {
            Log("WARN", $"Не удалось загрузить {url}: {ex.Message}");
            return null;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var title = Extract(doc, source.TitleSelector) ?? ExtractMeta(doc, "og:title") ?? "Без заголовка";
        var contentRaw = Extract(doc, source.ContentSelector) ?? ExtractMeta(doc, "og:description") ?? "";
        var image = ExtractImage(doc, source.ImageSelector) ?? ExtractMeta(doc, "og:image");
        var dateStr = Extract(doc, source.DateSelector);
        var date = TryParseDate(dateStr) ?? DateTime.UtcNow;

        Log("DEBUG", $"Заголовок: '{title}'");

        var words = (title + " " + contentRaw).ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matchedTags = tagMapping.Map(words).Select(name =>
            allTags.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .Where(t => t is not null).Select(t => t!).ToList();

        Log("INFO", $"Найдено тегов: {matchedTags.Count}");

        var article = new Article
        {
            Title       = title.Trim(),
            Content     = TagMappingService.StripHtml(contentRaw),
            ContentHtml = TagMappingService.SanitizeHtml(contentRaw),
            ImageUrl    = image,
            SourceUrl   = url,
            PublishedAt = date,
            StatusId    = statusId,
            RssSourceId = source.Id
        };

        if (matchedTags.Count > 0)
        {
            db.Articles.Add(article);
            await db.SaveChangesAsync();
            
            db.ArticleTags.AddRange(matchedTags.Select(t => new ArticleTag
            {
                ArticleId = article.Id,
                TagId     = t.Id
            }));
            await db.SaveChangesAsync();
            
            return null;
        }

        return article;
    }

    /// <summary>
    /// Скачивает HTML и декодирует его.
    /// Приоритет: Windows-1251 (для старых РФ сайтов), если выглядит битым -> UTF-8.
    /// </summary>
    private async Task<string> DownloadHtmlWithEncodingAsync(HttpClient http, string url)
    {
        // 1. Получаем сырые байты
        byte[] bytes = await http.GetByteArrayAsync(url);

        // 2. Пробуем декодировать как Windows-1251 (самая частая причина кракозябр)
        Encoding encodingWin1251 = Encoding.GetEncoding(1251);
        string textWin1251 = encodingWin1251.GetString(bytes);

        // 3. Проверка: если текст выглядит нормально (мало спецсимволов), возвращаем его
        if (!IsGarbled(textWin1251))
        {
            return textWin1251;
        }

        // 4. Если Windows-1251 выдал мусор, пробуем UTF-8
        string textUtf8 = Encoding.UTF8.GetString(bytes);
        
        // Если и UTF-8 мусор (маловероятно, но бывает), вернем хотя бы UTF-8
        // Или можно выбросить ошибку, но лучше вернуть то, что есть
        return textUtf8;
    }

    /// <summary>
    /// Простая эвристика: если в тексте много символов "" или непечатных символов,
    /// значит кодировка выбрана неверно.
    /// </summary>
    private bool IsGarbled(string text)
    {
        if (string.IsNullOrEmpty(text)) return true;

        // Считаем количество символов замены ()
        int replacementCount = 0;
        foreach (char c in text)
        {
            if (c == '\uFFFD') replacementCount++;
        }

        // Если больше 2% символов - это мусор
        if ((double)replacementCount / text.Length > 0.02)
        {
            return true;
        }

        // Дополнительная проверка: если видим специфические последовательности кракозябр
        // Например, частое повторение символов из диапазона Latin-1, которые выглядят как мусор в кириллице
        // Но проверки на  обычно достаточно.
        
        return false;
    }

    /// <summary>
    /// Сохраняет HTML строку в файл внутри wwwroot/debug
    /// </summary>
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