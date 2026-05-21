using System.Net;
using System.Net.Http.Json;
using CatshrediasNewsAPI.IntegrationTests.Support;
using CatshrediasNewsAPI.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CatshrediasNewsAPI.IntegrationTests;

public class ArticlesIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ArticlesIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateArticle_GoesToPendingReview()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(
            TestDataSeeder.AuthorEmail, TestDataSeeder.AuthorPassword);

        var response = await client.PostAsJsonAsync("/api/articles", new
        {
            title = "New submission",
            content = "Article text",
            sourceUrl = (string?)null,
            imageUrl = (string?)null,
            publishedAt = DateTime.UtcNow,
            tagIds = Array.Empty<int>()
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var article = await response.Content.ReadFromJsonAsync<ArticleResponse>();
        Assert.Equal("PendingReview", article!.Status);
    }

    [Fact]
    public async Task LikeAndSave_ToggleState()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(
            TestDataSeeder.AuthorEmail, TestDataSeeder.AuthorPassword);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var articleId = db.Articles.First(a => a.Title == "Published article").Id;

        var likeResponse = await client.PostAsync($"/api/articles/{articleId}/like", null);
        Assert.Equal(HttpStatusCode.NoContent, likeResponse.StatusCode);
        var isLiked = await client.GetFromJsonAsync<bool>($"/api/articles/{articleId}/liked");
        Assert.True(isLiked);

        var saveResponse = await client.PostAsync($"/api/articles/{articleId}/save", null);
        saveResponse.EnsureSuccessStatusCode();
        var saveBody = await saveResponse.Content.ReadFromJsonAsync<SaveResponse>();
        Assert.True(saveBody!.Saved);
    }

    [Fact]
    public async Task DeleteArticle_SoftDeletesForAuthor()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(
            TestDataSeeder.AuthorEmail, TestDataSeeder.AuthorPassword);

        var createResponse = await client.PostAsJsonAsync("/api/articles", new
        {
            title = "To delete",
            content = "temp",
            sourceUrl = (string?)null,
            imageUrl = (string?)null,
            publishedAt = DateTime.UtcNow,
            tagIds = Array.Empty<int>()
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ArticleResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/articles/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var myArticles = await client.GetFromJsonAsync<List<ArticleResponse>>("/api/articles/my");
        Assert.DoesNotContain(myArticles!, a => a.Id == created.Id);
    }

    private sealed record ArticleResponse(int Id, string Status);
    private sealed record SaveResponse(bool Saved);
}
