using System.Net;
using System.Net.Http.Json;
using CatshrediasNewsAPI.IntegrationTests.Support;
using CatshrediasNewsAPI.Services;

namespace CatshrediasNewsAPI.IntegrationTests;

public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_WithConfirmedUser_ReturnsToken()
    {
        await _factory.InitializeDatabaseAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestDataSeeder.AuthorEmail,
            password = TestDataSeeder.AuthorPassword
        });

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(json!.Token));
        Assert.Equal("author", json.User.Username);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await _factory.InitializeDatabaseAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestDataSeeder.AuthorEmail,
            password = "wrong"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithEnglishCulture_UsesEnCulture()
    {
        var client = _factory.CreateClient();
        var email = $"new_{Guid.NewGuid():N}@test.local";

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "newuser",
            email,
            password = "Password123!",
            culture = CultureHelper.English
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body?.Token);
    }

    private sealed record LoginResponse(string Token, UserPayload User);
    private sealed record UserPayload(string Username);
}
