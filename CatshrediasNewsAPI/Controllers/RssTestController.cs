using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Controllers;

[ApiController]
[Route("api/rss-test")]
public class RssTestController(RssParserService parser, AppDbContext db) : ControllerBase
{
    // ? FetchSource : вручную запускает парсинг одного RSS-источника по его Id
    // вызывается со страницы rss-test.html (Public — только для разработки)
    [HttpPost("fetch/{sourceId:int}")]
    public async Task<IActionResult> FetchSource(int sourceId)
    {
        var source = await db.RssSources.FindAsync(sourceId);
        if (source is null) return NotFound($"Источник с Id={sourceId} не найден.");

        await parser.ParseSourceAsync(source);
        return Ok(new { message = $"Парсинг источника «{source.Name}» завершён." });
    }

    // ? GetSources : возвращает список всех RSS-источников с количеством статей
    // вызывается со страницы rss-test.html
    [HttpGet("sources")]
    public async Task<IActionResult> GetSources()
    {
        var sources = await db.RssSources
            .Select(s => new
            {
                s.Id, s.Name, s.Url, s.IsTrusted, s.LastFetchedAt,
                ArticlesCount = db.Articles.Count(a => a.RssSourceId == s.Id)
            })
            .ToListAsync();
        return Ok(sources);
    }

    // ? GetRecentArticles : возвращает последние N статей из RSS-источника
    // вызывается со страницы rss-test.html
    [HttpGet("articles/{sourceId:int}")]
    public async Task<IActionResult> GetRecentArticles(int sourceId, [FromQuery] int limit = 10)
    {
        var articles = await db.Articles
            .Where(a => a.RssSourceId == sourceId)
            .Include(a => a.Status)
            .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
            .OrderByDescending(a => a.PublishedAt)
            .Take(limit)
            .Select(a => new
            {
                a.Id, a.Title, a.SourceUrl, a.PublishedAt,
                Status  = a.Status.Name,
                Tags    = a.ArticleTags.Select(at => at.Tag.Name).ToList(),
                Content = a.Content
            })
            .ToListAsync();

        var result = articles.Select(a => new
        {
            a.Id, a.Title, a.SourceUrl, a.PublishedAt, a.Status, a.Tags,
            ContentPreview = a.Content.Length > 200 ? a.Content[..200] + "…" : a.Content
        });

        return Ok(result);
    }

    // ? PreviewFeed : скачивает RSS-фид по URL и возвращает первые N заголовков без сохранения в БД
    // вызывается со страницы rss-test.html
    [HttpGet("preview")]
    public async Task<IActionResult> PreviewFeed([FromQuery] string url, [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(url)) return BadRequest("Укажите url.");
        try
        {
            var feed = await CodeHollow.FeedReader.FeedReader.ReadAsync(url);
            var items = feed.Items.Take(limit).Select(i => new
            {
                i.Title,
                i.Link,
                PublishedAt = i.PublishingDate,
                Categories  = i.Categories?.ToList() ?? []
            });
            return Ok(new { feedTitle = feed.Title, items });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
