using System.Security.Claims;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatshrediasNewsAPI.Controllers;

[ApiController]
[Route("api/articles/{articleId:int}/comments")]
public class CommentsController(CommentService commentService) : ControllerBase
{
    // ? GetByArticle : возвращает дерево комментариев для статьи
    // вызывается клиентом (Public)
    [HttpGet]
    public async Task<IActionResult> GetByArticle(int articleId)
    {
        return Ok(await commentService.GetByArticleAsync(articleId));
    }

    // ? Create : добавляет комментарий к статье
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(int articleId, CreateCommentDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var comment = await commentService.CreateAsync(articleId, userId, dto);
        return Ok(comment);
    }

    // ? Delete : удаляет комментарий (только автор или модератор)
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpDelete("{commentId:int}")]
    public async Task<IActionResult> Delete(int commentId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isModerator = User.IsInRole("Moderator") || User.IsInRole("Admin");
        var result = await commentService.DeleteAsync(commentId, userId, isModerator);
        return result ? NoContent() : NotFound();
    }
}
