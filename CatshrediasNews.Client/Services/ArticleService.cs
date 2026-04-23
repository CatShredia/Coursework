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

    // ? SaveDraftToDbAsync : сохраняет черновик в БД
    // вызывается из Pages/Publicist/CreateArticle.razor
    public async Task<(ArticleDto? article, string? error)> SaveDraftToDbAsync(
        int? articleId, string title, string content, List<int> tagIds, string? imageUrl)
    {
        var payload = new { articleId, title, content, imageUrl, tagIds };
        var res = await http.PostAsJsonAsync("api/articles/draft", payload);
        if (!res.IsSuccessStatusCode) return (null, "Ошибка при сохранении черновика.");
        var dto = await res.Content.ReadFromJsonAsync<ArticleDto>();
        return (dto, null);
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

    // ? IsLikedAsync : проверяет, лайкнул ли пользователь статью
    // вызывается из Pages/ArticleView.razor
    public async Task<bool> IsLikedAsync(int articleId)
    {
        var res = await http.GetFromJsonAsync<bool>($"api/articles/{articleId}/liked");
        return res;
    }

    // ? GetLikedIdsAsync : возвращает все Id лайкнутых статей
    // вызывается из Pages/Home.razor, Pages/Saved.razor
    public async Task<HashSet<int>> GetLikedIdsAsync() =>
        new(await http.GetFromJsonAsync<List<int>>("api/articles/liked-ids") ?? []);

    // ? GetSavedIdsAsync : возвращает все Id сохранённых статей
    // вызывается из Pages/Home.razor, Pages/Saved.razor
    public async Task<HashSet<int>> GetSavedIdsAsync() =>
        new(await http.GetFromJsonAsync<List<int>>("api/articles/saved-ids") ?? []);

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

    public async Task<List<ArticleDto>> SearchAsync(string query, string? source = null)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var url = $"api/articles/search?q={Uri.EscapeDataString(query)}";
        if (!string.IsNullOrWhiteSpace(source))
        {
            url += $"&source={Uri.EscapeDataString(source)}";
        }

        return await http.GetFromJsonAsync<List<ArticleDto>>(url) ?? [];
    }

    public async Task<List<ArticleDto>> GetSavedAsync()
    {
        return await http.GetFromJsonAsync<List<ArticleDto>>("api/articles/saved") ?? [];
    }

    public async Task<bool> ToggleSaveAsync(int articleId)
    {
        var res = await http.PostAsync($"api/articles/{articleId}/save", null);
        return res.IsSuccessStatusCode;
    }

    public async Task<TagDto?> CreateTagAsync(string name)
    {
        var res = await http.PostAsJsonAsync("api/tags", new { name });
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<TagDto>();
    }
}
