using System.Net.Http.Json;

namespace CatshrediasNews.Client.Services;

public class GigaChadService(HttpClient http)
{
    public Task<string?> ImproveAsync(string text)    => Post("api/gigachad/improve",     text);
    public Task<string?> ExpandAsync(string text)     => Post("api/gigachad/expand",      text);
    public Task<string?> ShortenAsync(string text)    => Post("api/gigachad/shorten",     text);
    public Task<string?> SpellcheckAsync(string text) => Post("api/gigachad/spellcheck",  text);
    public Task<string?> CheckRulesAsync(string text) => Post("api/gigachad/check-rules", text);

    private async Task<string?> Post(string url, string text)
    {
        try
        {
            var res = await http.PostAsJsonAsync(url, new { text });
            if (!res.IsSuccessStatusCode) return null;
            var obj = await res.Content.ReadFromJsonAsync<GigaResponse>();
            return obj?.Response;
        }
        catch { return null; }
    }

    private record GigaResponse(string Response);
}
