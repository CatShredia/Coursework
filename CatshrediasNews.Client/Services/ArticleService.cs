using System.Net.Http.Json;
using CatshrediasNews.Client.Models;

namespace CatshrediasNews.Client.Services;

file record UploadResult(string Url);

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

    // ? GetMyArticlesAsync : возвращает статьи текущего пользователя
    // вызывается из Pages/Publicist/MyArticles.razor
    public async Task<List<ArticleDto>> GetMyArticlesAsync()
    {
        return await http.GetFromJsonAsync<List<ArticleDto>>("api/articles/my") ?? [];
    }

    // ? DeleteArticleAsync : soft delete статьи
    // вызывается из Pages/Publicist/MyArticles.razor
    public async Task<bool> DeleteArticleAsync(int articleId)
    {
        var res = await http.DeleteAsync($"api/articles/{articleId}");
        return res.IsSuccessStatusCode;
    }

    // ? GetByIdAsync : возвращает одну статью по Id
    // вызывается из Pages/ArticleView.razor
    public async Task<ArticleDto?> GetByIdAsync(int id)
    {
        return await http.GetFromJsonAsync<ArticleDto>($"api/articles/{id}");
    }

    // ? SaveArticleAsync : создаёт новую статью или обновляет существующую
    // вызывается из Pages/Publicist/CreateArticle.razor
    public async Task<(ArticleDto? article, string? error)> SaveArticleAsync(
        int? id, string title, string content, List<int> tagIds, string? imageUrl = null)
    {
        var payload = new { title, content, tagIds, publishedAt = DateTime.UtcNow, sourceUrl = (string?)null, imageUrl };

        if (id is null)
        {
            var res = await http.PostAsJsonAsync("api/articles", payload);
            if (!res.IsSuccessStatusCode) return (null, "Ошибка при сохранении.");
            var dto = await res.Content.ReadFromJsonAsync<ArticleDto>();
            return (dto, null);
        }
        else
        {
            var res = await http.PutAsJsonAsync($"api/articles/{id}", payload);
            if (!res.IsSuccessStatusCode) return (null, "Ошибка при обновлении.");
            var dto = await res.Content.ReadFromJsonAsync<ArticleDto>();
            return (dto, null);
        }
    }

    // ? ToggleLikeAsync : ставит или снимает лайк на статью
    // вызывается из Pages/Home.razor
    public async Task<bool> ToggleLikeAsync(int articleId)
    {
        var response = await http.PostAsync($"api/articles/{articleId}/like", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<(string? url, string? error)> UploadImageAsync(MultipartFormDataContent form)
    {
        var res = await http.PostAsync("api/articles/upload-image", form);
        if (!res.IsSuccessStatusCode) return (null, "Ошибка загрузки.");
        var obj = await res.Content.ReadFromJsonAsync<UploadResult>();
        return (obj?.Url, null);
    }
}
