using System.Net.Http.Json;
using CatshrediasNews.Client.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace CatshrediasNews.Client.Services;

public class CommentService : IAsyncDisposable
{
    private readonly HttpClient _http;
    private HubConnection? _hub;
    private int _currentArticleId;

    public event Action<CommentDto>? OnCommentReceived;
    public event Action<int>?        OnCommentDeleted;

    public CommentService(HttpClient http) => _http = http;

    public async Task<List<CommentDto>> GetAsync(int articleId) =>
        await _http.GetFromJsonAsync<List<CommentDto>>(
            $"api/articles/{articleId}/comments") ?? [];

    public async Task ConnectAsync(int articleId)
    {
        if (_hub is not null)
            await DisconnectAsync();

        _currentArticleId = articleId;

        _hub = new HubConnectionBuilder()
            .WithUrl(new Uri(_http.BaseAddress!, "hubs/comments"), opts =>
            {
                var token = _http.DefaultRequestHeaders.Authorization?.Parameter;
                if (!string.IsNullOrEmpty(token))
                    opts.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .Build();

        _hub.On<CommentDto>("ReceiveComment", c => OnCommentReceived?.Invoke(c));
        _hub.On<int>("CommentDeleted",        id => OnCommentDeleted?.Invoke(id));

        await _hub.StartAsync();
        await _hub.InvokeAsync("JoinArticle", articleId);
    }

    public async Task DisconnectAsync()
    {
        if (_hub is null) return;
        try
        {
            await _hub.InvokeAsync("LeaveArticle", _currentArticleId);
            await _hub.StopAsync();
        }
        catch { /* игнорируем ошибки при отключении */ }
        await _hub.DisposeAsync();
        _hub = null;
    }

    public async Task<CommentDto?> SendAsync(int articleId, string content, int? parentId = null)
    {
        var res = await _http.PostAsJsonAsync(
            $"api/articles/{articleId}/comments",
            new { content, parentCommentId = parentId });
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<CommentDto>();
    }

    public async Task<bool> DeleteAsync(int articleId, int commentId)
    {
        var res = await _http.DeleteAsync($"api/articles/{articleId}/comments/{commentId}");
        return res.IsSuccessStatusCode;
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync();
}
