using System.Security.Claims;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatshrediasNewsAPI.Controllers;

[ApiController]
[Route("api/moderation")]
[Authorize(Roles = "Moderator,Admin")]
public class ModerationController(ModerationService moderationService) : ControllerBase
{
    // ? GetQueue : возвращает очередь статей на проверку
    // вызывается клиентом (Moderator)
    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue()
    {
        return Ok(await moderationService.GetQueueAsync());
    }

    // ? Approve : одобряет статью и публикует её
    // вызывается клиентом (Moderator)
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var moderatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await moderationService.ApproveAsync(id, moderatorId);
        return result ? NoContent() : NotFound();
    }

    // ? Reject : отклоняет статью с указанием причины
    // вызывается клиентом (Moderator)
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, RejectArticleDto dto)
    {
        var moderatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await moderationService.RejectAsync(id, moderatorId, dto);
        return result ? NoContent() : NotFound();
    }

    // ? GetReports : возвращает список всех жалоб пользователей
    // вызывается клиентом (Moderator)
    [HttpGet("reports")]
    public async Task<IActionResult> GetReports()
    {
        return Ok(await moderationService.GetReportsAsync());
    }

    // ? DismissReport : soft delete жалобы — устанавливает DeletedAt
    // вызывается клиентом (Moderator)
    [HttpDelete("reports/{id:int}")]
    public async Task<IActionResult> DismissReport(int id)
    {
        var result = await moderationService.DismissReportAsync(id);
        return result ? NoContent() : NotFound();
    }

    // ? CreateReport : создаёт жалобу на статью
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpPost("articles/{articleId:int}/report")]
    public async Task<IActionResult> CreateReport(int articleId, CreateReportDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await moderationService.CreateReportAsync(articleId, userId, dto);
        return NoContent();
    }
}
