using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Services;

public class ModerationService(AppDbContext db)
{
    // ? GetCountsAsync : возвращает количество статей в очереди и активных жалоб
    // вызывается из ModerationController.GetCounts (Moderator)
    public async Task<object> GetCountsAsync()
    {
        var queue   = await db.Articles.CountAsync(a => a.Status.Name == "PendingReview");
        var reports = await db.Reports.CountAsync();
        return new { queue, reports };
    }

    // ? GetQueueAsync : возвращает статьи со статусом PendingReview
    // вызывается из ModerationController.GetQueue (Moderator)
    public async Task<List<ArticleDto>> GetQueueAsync()
    {
        var articles = await db.Articles
            .Where(a => a.Status.Name == "PendingReview")
            .Include(a => a.Status)
            .Include(a => a.Author)
            .Include(a => a.RssSource)
            .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
            .ToListAsync();

        return articles.Select(a => MapToDto(a)).ToList();
    }

    // ? ApproveAsync : одобряет статью и записывает действие в журнал
    // вызывается из ModerationController.Approve (Moderator)
    public async Task<bool> ApproveAsync(int articleId, int moderatorId)
    {
        var article = await db.Articles.FindAsync(articleId);
        if (article is null) return false;

        var moderatorExists = await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == moderatorId);
        if (!moderatorExists) return false;

        var publishedStatus = await db.PublicationStatuses.FirstAsync(s => s.Name == "Published");
        article.StatusId = publishedStatus.Id;

        db.ModerationLogs.Add(new ModerationLog
        {
            ArticleId   = articleId,
            ModeratorId = moderatorId,
            Action      = "Approved"
        });

        await db.SaveChangesAsync();
        return true;
    }

    // ? GetQueueArticleAsync : возвращает статью из очереди по Id
    public async Task<ArticleDto?> GetQueueArticleAsync(int articleId)
    {
        var article = await db.Articles
            .Where(a => a.Id == articleId && a.Status.Name == "PendingReview")
            .Include(a => a.Status)
            .Include(a => a.Author)
            .Include(a => a.RssSource)
            .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
            .FirstOrDefaultAsync();

        return article is null ? null : MapToDto(article);
    }

    // ? RejectAsync : отклоняет статью с замечаниями по фрагментам
    public async Task<(bool Success, string? Error)> RejectAsync(int articleId, int moderatorId, RejectArticleDto dto)
    {
        if (dto.Notes is null || dto.Notes.Count == 0)
            return (false, "Добавьте хотя бы одно замечание.");

        var article = await db.Articles.FindAsync(articleId);
        if (article is null) return (false, null);

        var moderatorExists = await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == moderatorId);
        if (!moderatorExists) return (false, null);

        var noteEntities = new List<ModerationNote>();
        for (var i = 0; i < dto.Notes.Count; i++)
        {
            var excerpt = (dto.Notes[i].Excerpt ?? "").Trim();
            var reason  = (dto.Notes[i].Reason ?? "").Trim();
            if (excerpt.Length < 3)
                return (false, "Фрагмент слишком короткий.");
            if (reason.Length < 3)
                return (false, "Укажите причину для каждого замечания.");
            if (excerpt.Length > 2000) excerpt = excerpt[..2000];
            if (reason.Length > 1000) reason = reason[..1000];

            noteEntities.Add(new ModerationNote
            {
                Excerpt   = excerpt,
                Reason    = reason,
                SortOrder = i
            });
        }

        var rejectedStatus = await db.PublicationStatuses.FirstAsync(s => s.Name == "Rejected");
        article.StatusId = rejectedStatus.Id;

        var summary = string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim();
        if (summary is null && noteEntities.Count > 0)
            summary = noteEntities.Count == 1 ? "1 замечание" : $"{noteEntities.Count} замечания";

        var log = new ModerationLog
        {
            ArticleId   = articleId,
            ModeratorId = moderatorId,
            Action      = "Rejected",
            Reason      = summary,
            Notes       = noteEntities
        };
        db.ModerationLogs.Add(log);

        await db.SaveChangesAsync();
        return (true, null);
    }

    // ? GetReportsAsync : возвращает список всех жалоб пользователей
    // вызывается из ModerationController.GetReports (Moderator)
    public async Task<List<ReportDto>> GetReportsAsync()
    {
        return await db.Reports
            .Include(r => r.User)
            .Include(r => r.ReportType)
            .Select(r => new ReportDto(
                r.Id, r.ReportType.Name, r.Description,
                r.User.Username, r.CreatedAt, r.ArticleId))
            .ToListAsync();
    }

    // ? CreateReportAsync : создаёт жалобу на статью
    // вызывается из ModerationController.CreateReport (Auth)
    public async Task CreateReportAsync(int articleId, int userId, CreateReportDto dto)
    {
        db.Reports.Add(new Report
        {
            ArticleId = articleId,
            UserId = userId,
            ReportTypeId = dto.ReportTypeId,
            Description = dto.Description
        });
        await db.SaveChangesAsync();
    }

    // ? DismissReportAsync : soft delete жалобы — устанавливает DeletedAt
    // вызывается из ModerationController.DismissReport (Moderator)
    public async Task<bool> DismissReportAsync(int reportId)
    {
        var report = await db.Reports.FindAsync(reportId);
        if (report is null) return false;
        report.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    private static ArticleDto MapToDto(Article a) => new(
        a.Id, a.Title, a.Content, a.ContentHtml, a.ImageUrl, a.RssAuthor,
        a.SourceUrl, a.PublishedAt,
        a.Status.Name, a.AuthorId, a.Author?.Username,
        a.ArticleTags.Select(at => at.Tag.Name).ToList(),
        0, a.RssSource?.Name,
        null
    );
}
