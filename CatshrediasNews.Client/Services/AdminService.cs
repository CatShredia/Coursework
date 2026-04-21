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
