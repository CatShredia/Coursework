using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.IntegrationTests.Support;

public static class TestDataSeeder
{
    public const string AuthorEmail = "author@test.local";
    public const string AuthorPassword = "Author123!";
    public const string ModeratorEmail = "moderator@test.local";
    public const string ModeratorPassword = "Moderator123!";

    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (await db.Users.AnyAsync())
            return;

        var userRole = await db.Roles.FirstAsync(r => r.Name == "User");
        var moderatorRole = await db.Roles.FirstAsync(r => r.Name == "Moderator");

        var author = new User
        {
            Username = "author",
            Email = AuthorEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(AuthorPassword),
            RoleId = userRole.Id,
            EmailConfirmed = true
        };

        var moderator = new User
        {
            Username = "moderator",
            Email = ModeratorEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(ModeratorPassword),
            RoleId = moderatorRole.Id,
            EmailConfirmed = true
        };

        db.Users.AddRange(author, moderator);
        await db.SaveChangesAsync();

        var pendingStatus = await db.PublicationStatuses.FirstAsync(s => s.Name == "PendingReview");
        var publishedStatus = await db.PublicationStatuses.FirstAsync(s => s.Name == "Published");

        var pendingArticle = new Article
        {
            Title = "Pending article",
            Content = "Body for moderation",
            PublishedAt = DateTime.UtcNow,
            StatusId = pendingStatus.Id,
            AuthorId = author.Id
        };

        var publishedArticle = new Article
        {
            Title = "Published article",
            Content = "Public body",
            PublishedAt = DateTime.UtcNow.AddHours(-1),
            StatusId = publishedStatus.Id,
            AuthorId = author.Id
        };

        db.Articles.AddRange(pendingArticle, publishedArticle);
        await db.SaveChangesAsync();
    }
}
