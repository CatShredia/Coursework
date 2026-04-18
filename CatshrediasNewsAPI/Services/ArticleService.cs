using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Services;

public class ArticleService(AppDbContext db)
{
    // ? GetFeedAsync : возвращает ленту статей с учётом весов тегов пользователя
    // вызывается из ArticlesController.GetFeed (Auth)
    public async Task<List<ArticleDto>> GetFeedAsync(int userId, int page, int pageSize)
    {
        var weights = await db.UserTagWeights
            .Where(utw => utw.UserId == userId)
            .ToDictionaryAsync(utw => utw.TagId, utw => utw.Weight);

        var articles = await db.Articles
            .Where(a => a.Status.Name == "Published")
            .Include(a => a.Status)
            .Include(a => a.Author)
            .Include(a => a.RssSource)
            .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
            .Include(a => a.Likes)
            .ToListAsync();

        return articles
            .OrderByDescending(a => a.ArticleTags.Sum(at =>
                weights.TryGetValue(at.TagId, out var w) ? w : 0))
            .ThenByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToDto)
            .ToList();
    }

    // ? GetPublicFeedAsync : возвращает общую хронологическую ленту опубликованных статей
    // вызывается из ArticlesController.GetPublicFeed (Public)
    public async Task<List<ArticleDto>> GetPublicFeedAsync(int page, int pageSize)
    {
        return await db.Articles
            .Where(a => a.Status.Name == "Published")
            .Include(a => a.Status)
            .Include(a => a.Author)
            .Include(a => a.RssSource)
            .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
            .Include(a => a.Likes)
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    // ? GetByIdAsync : возвращает статью по идентификатору
    // вызывается из ArticlesController.GetById (Public)
    public async Task<ArticleDto?> GetByIdAsync(int id)
    {
        var article = await db.Articles
            .Where(a => a.Id == id)
            .Include(a => a.Status)
            .Include(a => a.Author)
            .Include(a => a.RssSource)
            .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
            .Include(a => a.Likes)
            .FirstOrDefaultAsync();

        return article is null ? null : MapToDto(article);
    }

    // ? LikeAsync : ставит или снимает лайк, обновляет вес тега пользователя
    // вызывается из ArticlesController.Like (Auth)
    public async Task<bool> LikeAsync(int articleId, int userId)
    {
        var existing = await db.Likes.FindAsync(userId, articleId);
        if (existing is not null)
        {
            db.Likes.Remove(existing);
            await UpdateTagWeightsAsync(articleId, userId, -0.5f);
        }
        else
        {
            db.Likes.Add(new Like { ArticleId = articleId, UserId = userId });
            await UpdateTagWeightsAsync(articleId, userId, +0.5f);
        }

        await db.SaveChangesAsync();
        return true;
    }

    // ? CreateAsync : создаёт новую статью со статусом PendingReview
    // вызывается из ArticlesController.Create (Auth)
    public async Task<ArticleDto> CreateAsync(int authorId, CreateArticleDto dto)
    {
        var pendingStatus = await db.PublicationStatuses.FirstAsync(s => s.Name == "PendingReview");

        var article = new Article
        {
            Title = dto.Title,
            Content = dto.Content,
            SourceUrl = dto.SourceUrl,
            PublishedAt = dto.PublishedAt,
            AuthorId = authorId,
            StatusId = pendingStatus.Id
        };

        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var tags = dto.TagIds.Select(tagId => new ArticleTag { ArticleId = article.Id, TagId = tagId });
        db.ArticleTags.AddRange(tags);
        await db.SaveChangesAsync();

        return (await GetByIdAsync(article.Id))!;
    }

    // ? UpdateAsync : обновляет статью автора
    // вызывается из ArticlesController.Update (Auth)
    public async Task<ArticleDto?> UpdateAsync(int articleId, int authorId, UpdateArticleDto dto)
    {
        var article = await db.Articles
            .Include(a => a.ArticleTags)
            .FirstOrDefaultAsync(a => a.Id == articleId && a.AuthorId == authorId);
        if (article is null) return null;

        article.Title   = dto.Title;
        article.Content = dto.Content;

        db.ArticleTags.RemoveRange(article.ArticleTags);
        db.ArticleTags.AddRange(dto.TagIds.Select(t => new ArticleTag { ArticleId = article.Id, TagId = t }));

        await db.SaveChangesAsync();
        return await GetByIdAsync(article.Id);
    }

    // ? GetByAuthorAsync : возвращает все статьи автора внезависимо от статуса
    // вызывается из ArticlesController.GetMyArticles (Auth)
    public async Task<List<ArticleDto>> GetByAuthorAsync(int authorId)
    {
        return await db.Articles
            .Where(a => a.AuthorId == authorId)
            .Include(a => a.Status)
            .Include(a => a.RssSource)
            .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
            .Include(a => a.Likes)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    // ? SoftDeleteAsync : soft delete статьи — устанавливает DeletedAt
    // вызывается из ArticlesController.Delete (Auth/Admin)
    public async Task<bool> SoftDeleteAsync(int articleId)
    {
        var article = await db.Articles.FindAsync(articleId);
        if (article is null) return false;
        article.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    // ? UpdateTagWeightsAsync : увеличивает или уменьшает вес тегов статьи для пользователя
    // вызывается из LikeAsync
    private async Task UpdateTagWeightsAsync(int articleId, int userId, float delta)
    {
        var tagIds = await db.ArticleTags
            .Where(at => at.ArticleId == articleId)
            .Select(at => at.TagId)
            .ToListAsync();

        foreach (var tagId in tagIds)
        {
            var weight = await db.UserTagWeights.FindAsync(userId, tagId);
            if (weight is null)
                db.UserTagWeights.Add(new UserTagWeight { UserId = userId, TagId = tagId, Weight = 1f + delta });
            else
                weight.Weight = Math.Max(0, weight.Weight + delta);
        }
    }

    private static ArticleDto MapToDto(Article a) => new(
        a.Id, a.Title, a.Content, a.ContentHtml, a.ImageUrl, a.RssAuthor,
        a.SourceUrl, a.PublishedAt,
        a.Status.Name, a.AuthorId, a.Author?.Username,
        a.ArticleTags.Select(at => at.Tag.Name).ToList(),
        a.Likes.Count,
        a.RssSource?.Name
    );
}
