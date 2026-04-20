using CodeHollow.FeedReader;
using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Services;

public class RssParserService(IServiceScopeFactory scopeFactory, TagMappingService tagMapping, ILogger<RssParserService> logger)
{
    // ? ParseSourceAsync : скачивает RSS-фид, парсит статьи и сохраняет новые в БД
    // вызывается из RssFetcherService.ExecuteAsync
    public async Task ParseSourceAsync(RssSource source)
    {
        logger.LogInformation("Парсинг RSS-источника: {Name} ({Url})", source.Name, source.Url);

        Feed feed;
        try
        {
            feed = await FeedReader.ReadAsync(source.Url);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при загрузке фида {Url}", source.Url);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var status = await db.PublicationStatuses.FirstAsync(s =>
            s.Name == (source.IsTrusted ? "Published" : "PendingReview"));

        var allTags = await db.Tags.ToListAsync();

        int created = 0;

        foreach (var item in feed.Items)
        {
            var guid = item.Id ?? item.Link;
            if (string.IsNullOrWhiteSpace(guid)) continue;

            // Пропускаем дубли по SourceUrl
            if (await db.Articles.AnyAsync(a => a.SourceUrl == guid)) continue;

            var rssCategories = item.Categories ?? [];
            var matchedTagNames = tagMapping.Map(rssCategories);
            var matchedTags = allTags
                .Where(t => matchedTagNames.Contains(t.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var rawHtml = (item.SpecificItem as CodeHollow.FeedReader.Feeds.MediaRssFeedItem)?.Content
                       ?? (item.SpecificItem as CodeHollow.FeedReader.Feeds.Rss20FeedItem)?.Content
                       ?? item.Description
                       ?? string.Empty;

            var article = new Article
            {
                Title       = item.Title ?? "(без заголовка)",
                Content     = TagMappingService.StripHtml(rawHtml),
                ContentHtml = TagMappingService.SanitizeHtml(rawHtml),
                ImageUrl    = TagMappingService.ExtractFirstImage(rawHtml),
                RssAuthor   = item.Author,
                SourceUrl   = guid,
                PublishedAt = item.PublishingDate?.ToUniversalTime() ?? DateTime.UtcNow,
                StatusId    = status.Id,
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

            created++;
        }

        source.LastFetchedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("Источник {Name}: добавлено {Count} новых статей.", source.Name, created);
    }
}
