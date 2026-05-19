using System.Net;
using System.Net.Http.Json;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.IntegrationTests.Support;
using CatshrediasNewsAPI.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CatshrediasNewsAPI.IntegrationTests;

public class ModerationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ModerationIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Reject_ThenAuthorSeesRejectionNotes()
    {
        var moderatorClient = await _factory.CreateAuthenticatedClientAsync(
            TestDataSeeder.ModeratorEmail, TestDataSeeder.ModeratorPassword);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var authorId = await ArticleTestHelper.GetAuthorIdAsync(db);
        var articleId = await ArticleTestHelper.CreatePendingArticleAsync(db, authorId, $"Reject_{Guid.NewGuid():N}");

        var rejectDto = new RejectArticleDto(
            "Needs revision",
            [new ModerationNoteDto("problem excerpt", "grammar")]);

        var rejectResponse = await moderatorClient.PostAsJsonAsync(
            $"/api/moderation/{articleId}/reject", rejectDto);
        Assert.Equal(HttpStatusCode.NoContent, rejectResponse.StatusCode);

        var authorClient = await _factory.CreateAuthenticatedClientAsync(
            TestDataSeeder.AuthorEmail, TestDataSeeder.AuthorPassword);

        var myArticles = await authorClient.GetFromJsonAsync<List<ArticleResponse>>("/api/articles/my");
        var rejected = myArticles!.First(a => a.Id == articleId);

        Assert.Equal("Rejected", rejected.Status);
        Assert.Equal("Needs revision", rejected.RejectionReason);
        Assert.NotNull(rejected.RejectionNotes);
        Assert.Contains(rejected.RejectionNotes!, n => n.Excerpt == "problem excerpt");
    }

    [Fact]
    public async Task Approve_PublishesArticle()
    {
        var moderatorClient = await _factory.CreateAuthenticatedClientAsync(
            TestDataSeeder.ModeratorEmail, TestDataSeeder.ModeratorPassword);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var authorId = await ArticleTestHelper.GetAuthorIdAsync(db);
        var articleId = await ArticleTestHelper.CreatePendingArticleAsync(db, authorId, $"Approve_{Guid.NewGuid():N}");

        var approveResponse = await moderatorClient.PostAsync(
            $"/api/moderation/{articleId}/approve", null);
        Assert.Equal(HttpStatusCode.NoContent, approveResponse.StatusCode);

        var publicClient = _factory.CreateClient();
        var article = await publicClient.GetFromJsonAsync<ArticleResponse>($"/api/articles/{articleId}");
        Assert.Equal("Published", article!.Status);
    }

    private sealed record ArticleResponse(
        int Id,
        string Status,
        string? RejectionReason,
        List<ModerationNoteDto>? RejectionNotes);
}
