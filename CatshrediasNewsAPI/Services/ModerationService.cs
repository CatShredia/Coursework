using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Services;

public class ModerationService(AppDbContext db)
{
    // ? GetQueueAsync : возвращает статьи со статусом PendingReview
    // вызывается из ModerationController.GetQueue (Moderator)
    public async Task<List<ArticleDto>> GetQueueAsync()
    {
        return await db.Articles
            .Where(a => a.Status.Name == "PendingReview")
            .Include(a => a.Status)
            .Include(a => a.Author)
            .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    // ? ApproveAsync : одобряет статью и записывает действие в журнал
    // вызывается из ModerationController.Approve (Moderator)
    public async Task<bool> ApproveAsync(int articleId, int moderatorId)
    {
        var article = await db.Articles.FindAsync(articleId);
        if (article is null) return false;

        var publishedStatus = await db.PublicationStatuses.FirstAsync(s => s.Name == "Published");
        article.StatusId = publishedStatus.Id;

        db.ModerationLogs.Add(new ModerationLog
        {
            ArticleId = articleId,
            ModeratorId = moderatorId,
            Action = "Approved"
        });

        await db.SaveChangesAsync();
        return true;
    }

    // ? RejectAsync : отклоняет статью с указанием причины и записывает в журнал
    // вызывается из ModerationController.Reject (Moderator)
    public async Task<bool> RejectAsync(int articleId, int moderatorId, RejectArticleDto dto)
    {
        var article = await db.Articles.FindAsync(articleId);
        if (article is null) return false;

        var rejectedStatus = await db.PublicationStatuses.FirstAsync(s => s.Name == "Rejected");
        article.StatusId = rejectedStatus.Id;

        db.ModerationLogs.Add(new ModerationLog
        {
            ArticleId = articleId,
            ModeratorId = moderatorId,
            Action = "Rejected",
            Reason = dto.Reason
        });

        await db.SaveChangesAsync();
        return true;
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
        a.Id, a.Title, a.Content, a.SourceUrl, a.PublishedAt,
        a.Status.Name, a.Author?.Username,
        a.ArticleTags.Select(at => at.Tag.Name).ToList(),
        0
    );
}
