using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.IntegrationTests.Support;

public static class ArticleTestHelper
{
    public static async Task<int> CreatePendingArticleAsync(AppDbContext db, int authorId, string title)
    {
        var status = await db.PublicationStatuses.FirstAsync(s => s.Name == "PendingReview");
        var article = new Article
        {
            Title = title,
            Content = "Test content",
            PublishedAt = DateTime.UtcNow,
            StatusId = status.Id,
            AuthorId = authorId
        };
        db.Articles.Add(article);
        await db.SaveChangesAsync();
        return article.Id;
    }

    public static async Task<int> GetAuthorIdAsync(AppDbContext db) =>
        (await db.Users.FirstAsync(u => u.Email == TestDataSeeder.AuthorEmail)).Id;
}
