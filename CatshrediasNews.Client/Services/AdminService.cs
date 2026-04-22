using System.Net.Http.Json;
using CatshrediasNews.Client.Models;

namespace CatshrediasNews.Client.Services;

public class AdminService(HttpClient http)
{
    // ? GetUsersAsync : возвращает список всех пользователей
    // вызывается из Pages/Admin/Users.razor (Admin)
    public async Task<List<UserInfo>> GetUsersAsync()
    {
        return await http.GetFromJsonAsync<List<UserInfo>>("api/admin/users") ?? [];
    }

    // ? SetRoleAsync : меняет роль пользователя
    // вызывается из Pages/Admin/Users.razor (Admin)
    public async Task<bool> SetRoleAsync(int userId, string role)
    {
        var res = await http.PutAsJsonAsync($"api/admin/users/{userId}/role", role);
        return res.IsSuccessStatusCode;
    }

    // ? BlockUserAsync : блокирует пользователя по Id
    // вызывается из Pages/Admin/Users.razor (Admin)
    public async Task<bool> BlockUserAsync(int userId)
    {
        var res = await http.PostAsync($"api/admin/users/{userId}/block", null);
        return res.IsSuccessStatusCode;
    }

    // ? UnblockUserAsync : разблокирует пользователя по Id
    // вызывается из Pages/Admin/Users.razor (Admin)
    public async Task<bool> UnblockUserAsync(int userId)
    {
        var res = await http.PostAsync($"api/admin/users/{userId}/unblock", null);
        return res.IsSuccessStatusCode;
    }

    // ? DeleteUserAsync : удаляет пользователя по Id
    // вызывается из Pages/Admin/Users.razor (Admin)
    public async Task<bool> DeleteUserAsync(int userId)
    {
        var res = await http.DeleteAsync($"api/admin/users/{userId}");
        return res.IsSuccessStatusCode;
    }

    // ? GetSourcesAsync : возвращает список RSS-источников
    // вызывается из Pages/Admin/Rss.razor (Admin)
    public async Task<List<RssSourceDto>> GetSourcesAsync() =>
        await http.GetFromJsonAsync<List<RssSourceDto>>("api/admin/sources") ?? [];

    // ? CreateSourceAsync : добавляет новый RSS-источник
    // вызывается из Pages/Admin/Rss.razor (Admin)
    public async Task<RssSourceDto?> CreateSourceAsync(string name, string url, bool isTrusted,
        string sourceType = "Rss", string? linkSelector = null, string? titleSelector = null,
        string? contentSelector = null, string? dateSelector = null, string? imageSelector = null)
    {
        var res = await http.PostAsJsonAsync("api/admin/sources",
            new { name, url, isTrusted, sourceType, linkSelector, titleSelector, contentSelector, dateSelector, imageSelector });
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<RssSourceDto>();
    }

    public async Task<bool> UpdateSourceAsync(int id, string name, string url, bool isTrusted,
        string sourceType = "Rss", string? linkSelector = null, string? titleSelector = null,
        string? contentSelector = null, string? dateSelector = null, string? imageSelector = null)
    {
        var res = await http.PutAsJsonAsync($"api/admin/sources/{id}",
            new { name, url, isTrusted, sourceType, linkSelector, titleSelector, contentSelector, dateSelector, imageSelector });
        return res.IsSuccessStatusCode;
    }

    // ? DeleteSourceAsync : удаляет RSS-источник
    // вызывается из Pages/Admin/Rss.razor (Admin)
    public async Task<bool> DeleteSourceAsync(int id) =>
        (await http.DeleteAsync($"api/admin/sources/{id}")).IsSuccessStatusCode;

    // ? SetSourceEnabledAsync : включает или отключает RSS-источник
    // вызывается из Pages/Admin/Rss.razor (Admin)
    public async Task<bool> SetSourceEnabledAsync(int id, bool enabled)
    {
        var action = enabled ? "enable" : "disable";
        var res = await http.PostAsync($"api/admin/sources/{id}/{action}", null);
        return res.IsSuccessStatusCode;
    }

    // ? TriggerRssAsync : запускает принудительный парсинг
    // вызывается из Pages/Admin/Rss.razor (Admin)
    public async Task<bool> TriggerRssAsync() =>
        (await http.PostAsync("api/admin/rss/trigger", null)).IsSuccessStatusCode;

    // ? GetRssStatusAsync : возвращает текущий интервал парсинга
    // вызывается из Pages/Admin/Rss.razor (Admin)
    public async Task<int> GetRssIntervalAsync()
    {
        var res = await http.GetFromJsonAsync<RssStatusDto>("api/admin/rss/status");
        return res?.IntervalMinutes ?? 15;
    }

    // ? SetRssIntervalAsync : изменяет интервал парсинга
    // вызывается из Pages/Admin/Rss.razor (Admin)
    public async Task<bool> SetRssIntervalAsync(int minutes)
    {
        var res = await http.PutAsJsonAsync("api/admin/rss/interval", new { intervalMinutes = minutes });
        return res.IsSuccessStatusCode;
    }

    // ? GetTagsAsync : возвращает список всех тегов
    // вызывается из Pages/Admin/Tags.razor (Admin)
    public async Task<List<TagDto>> GetTagsAsync()
    {
        return await http.GetFromJsonAsync<List<TagDto>>("api/admin/tags") ?? [];
    }

    // ? CreateTagAsync : создаёт новый тег
    // вызывается из Pages/Admin/Tags.razor (Admin)
    public async Task<TagDto?> CreateTagAsync(string name)
    {
        var res = await http.PostAsJsonAsync("api/admin/tags", new { name });
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<TagDto>();
    }

    // ? DeleteTagAsync : удаляет тег по Id
    // вызывается из Pages/Admin/Tags.razor (Admin)
    public async Task<bool> DeleteTagAsync(int id)
    {
        var res = await http.DeleteAsync($"api/admin/tags/{id}");
        return res.IsSuccessStatusCode;
    }
}
