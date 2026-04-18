using System.Net.Http.Json;
using CatshrediasNews.Client.Models;

namespace CatshrediasNews.Client.Services;

public class ArticleService(HttpClient http)
{
    // ? GetFeedAsync : возвращает персональную ленту (Auth) или общую (Public)
    // вызывается из Pages/Home.razor
    public async Task<List<ArticleDto>> GetFeedAsync(bool authenticated, int page, int pageSize)
    {
        var url = authenticated
            ? $"api/articles/feed?page={page}&pageSize={pageSize}"
            : $"api/articles?page={page}&pageSize={pageSize}";

        return await http.GetFromJsonAsync<List<ArticleDto>>(url) ?? [];
    }

    // ? GetTagsAsync : возвращает список всех тегов для фильтрации
    // вызывается из Pages/Home.razor
    public async Task<List<TagDto>> GetTagsAsync()
    {
        return await http.GetFromJsonAsync<List<TagDto>>("api/tags") ?? [];
    }

    // ? ToggleLikeAsync : ставит или снимает лайк на статью
    // вызывается из Pages/Home.razor
    public async Task<bool> ToggleLikeAsync(int articleId)
    {
        var response = await http.PostAsync($"api/articles/{articleId}/like", null);
        return response.IsSuccessStatusCode;
    }
}
