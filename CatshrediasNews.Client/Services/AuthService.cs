using System.Net.Http.Json;
using CatshrediasNews.Client.Models;
using Microsoft.JSInterop;

namespace CatshrediasNews.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    public UserInfo? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;

    public event Action? OnChange;

    public AuthService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    // ? InitAsync : восстанавливает сессию из localStorage при загрузке приложения
    // вызывается из Program.cs
    public async Task InitAsync()
    {
        var token    = await _js.InvokeAsync<string?>("localStorage.getItem", "auth_token");
        var username = await _js.InvokeAsync<string?>("localStorage.getItem", "auth_username");
        var email    = await _js.InvokeAsync<string?>("localStorage.getItem", "auth_email");
        var role     = await _js.InvokeAsync<string?>("localStorage.getItem", "auth_role");
        var idStr    = await _js.InvokeAsync<string?>("localStorage.getItem", "auth_id");

        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(username))
        {
            CurrentUser = new UserInfo
            {
                Id       = int.TryParse(idStr, out var id) ? id : 0,
                Username = username,
                Email    = email ?? "",
                Role     = role ?? "User"
            };
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    // ? LoginAsync : отправляет запрос на вход и сохраняет токен в localStorage
    // вызывается из Pages/Login.razor
    public async Task<string?> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        if (!response.IsSuccessStatusCode)
            return "Неверный email или пароль.";

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (result is null) return "Ошибка сервера.";

        await SaveSessionAsync(result);
        return null;
    }

    // ? RegisterAsync : отправляет запрос на регистрацию и сохраняет токен
    // вызывается из Pages/Register.razor
    public async Task<string?> RegisterAsync(RegisterRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request);
        if (!response.IsSuccessStatusCode)
            return "Пользователь с таким email уже существует.";

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (result is null) return "Ошибка сервера.";

        await SaveSessionAsync(result);
        return null;
    }

    // ? UpdateProfileAsync : обновляет username, email или пароль текущего пользователя
    // вызывается из Pages/Profile.razor
    public async Task<string?> UpdateProfileAsync(string? username, string? email, string? password)
    {
        var payload  = new { username, email, password };
        var response = await _http.PutAsJsonAsync("api/users/me", payload);
        if (!response.IsSuccessStatusCode)
            return "Не удалось обновить профиль. Возможно, email уже занят.";

        var updated = await response.Content.ReadFromJsonAsync<UserInfo>();
        if (updated is null) return "Ошибка сервера.";

        CurrentUser = updated;
        await _js.InvokeVoidAsync("localStorage.setItem", "auth_username", updated.Username);
        await _js.InvokeVoidAsync("localStorage.setItem", "auth_email", updated.Email);
        OnChange?.Invoke();
        return null;
    }

    // ? DeleteAccountAsync : удаляет аккаунт, очищает сессию БЕЗ вызова OnChange
    // OnChange вызывается позже из Home.razor через NotifyChanged()
    // вызывается из Pages/Profile.razor
    public async Task<string?> DeleteAccountAsync()
    {
        Console.WriteLine("[AuthService] DeleteAccountAsync: start");
        var response = await _http.DeleteAsync("api/users/me");
        Console.WriteLine($"[AuthService] DeleteAccountAsync: status={response.StatusCode}");

        if (!response.IsSuccessStatusCode)
            return "Не удалось удалить аккаунт.";

        CurrentUser = null;
        _http.DefaultRequestHeaders.Authorization = null;
        await _js.InvokeVoidAsync("localStorage.removeItem", "auth_token");
        await _js.InvokeVoidAsync("localStorage.removeItem", "auth_username");
        await _js.InvokeVoidAsync("localStorage.removeItem", "auth_email");
        await _js.InvokeVoidAsync("localStorage.removeItem", "auth_role");
        await _js.InvokeVoidAsync("localStorage.removeItem", "auth_id");
        Console.WriteLine("[AuthService] DeleteAccountAsync: session cleared, no OnChange fired");
        return null;
    }

    // ? NotifyChanged : вызывает OnChange вручную — для обновления header после удаления аккаунта
    // вызывается из Pages/Home.razor
    public void NotifyChanged() => OnChange?.Invoke();

    // ? LogoutAsync : очищает сессию из localStorage и сбрасывает текущего пользователя
    // вызывается из Pages/Profile.razor
    public async Task LogoutAsync()
    {
        CurrentUser = null;
        _http.DefaultRequestHeaders.Authorization = null;
        await _js.InvokeVoidAsync("localStorage.removeItem", "auth_token");
        await _js.InvokeVoidAsync("localStorage.removeItem", "auth_username");
        await _js.InvokeVoidAsync("localStorage.removeItem", "auth_email");
        await _js.InvokeVoidAsync("localStorage.removeItem", "auth_role");
        await _js.InvokeVoidAsync("localStorage.removeItem", "auth_id");
        OnChange?.Invoke();
    }

    private async Task SaveSessionAsync(AuthResponse result)
    {
        CurrentUser = result.User;
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Token);

        await _js.InvokeVoidAsync("localStorage.setItem", "auth_token",    result.Token);
        await _js.InvokeVoidAsync("localStorage.setItem", "auth_username",  result.User.Username);
        await _js.InvokeVoidAsync("localStorage.setItem", "auth_email",     result.User.Email);
        await _js.InvokeVoidAsync("localStorage.setItem", "auth_role",      result.User.Role);
        await _js.InvokeVoidAsync("localStorage.setItem", "auth_id",        result.User.Id.ToString());
        OnChange?.Invoke();
    }
}
