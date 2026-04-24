using System.Net.Http.Json;
using CatshrediasNews.Client.Models;

namespace CatshrediasNews.Client.Services;

public class ModerationService(HttpClient http)
{
    public event Action? CountsChanged;

    public async Task<ModerationCounts> GetCountsAsync()
    {
        return await http.GetFromJsonAsync<ModerationCounts>("api/moderation/counts") ?? new();
    }

    public async Task<List<ArticleDto>> GetQueueAsync()
    {
        return await http.GetFromJsonAsync<List<ArticleDto>>("api/moderation/queue") ?? [];
    }

    public async Task<bool> ApproveAsync(int id)
    {
        var res = await http.PostAsync($"api/moderation/{id}/approve", null);
        if (res.IsSuccessStatusCode) CountsChanged?.Invoke();
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> RejectAsync(int id, string reason)
    {
        var res = await http.PostAsJsonAsync($"api/moderation/{id}/reject", new { reason });
        if (res.IsSuccessStatusCode) CountsChanged?.Invoke();
        return res.IsSuccessStatusCode;
    }

    public async Task<List<ReportDto>> GetReportsAsync()
    {
        return await http.GetFromJsonAsync<List<ReportDto>>("api/moderation/reports") ?? [];
    }

    public async Task<bool> DismissReportAsync(int id)
    {
        var res = await http.DeleteAsync($"api/moderation/reports/{id}");
        if (res.IsSuccessStatusCode) CountsChanged?.Invoke();
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> CreateReportAsync(int articleId, int reportTypeId, string? description)
    {
        var res = await http.PostAsJsonAsync($"api/moderation/articles/{articleId}/report",
            new { reportTypeId, description });
        return res.IsSuccessStatusCode;
    }
}
