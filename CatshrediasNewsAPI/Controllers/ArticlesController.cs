using System.Security.Claims;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatshrediasNewsAPI.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController(ArticleService articleService) : ControllerBase
{
    // ? GetMyArticles : возвращает статьи текущего пользователя
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyArticles()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await articleService.GetByAuthorAsync(userId));
    }

    // ? GetPublicFeed : возвращает хронологическую ленту опубликованных статей
    // вызывается клиентом (Public)
    [HttpGet]
    public async Task<IActionResult> GetPublicFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await articleService.GetPublicFeedAsync(page, pageSize));
    }

    // ? GetFeed : возвращает персонализированную ленту с учётом интересов пользователя
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await articleService.GetFeedAsync(userId, page, pageSize));
    }

    // ? GetById : возвращает детальную страницу статьи
    // вызывается клиентом (Public)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var article = await articleService.GetByIdAsync(id);
        return article is null ? NotFound() : Ok(article);
    }

    // ? Create : создаёт новую статью со статусом PendingReview
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateArticleDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var article = await articleService.CreateAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);
    }

    // ? Like : ставит или снимает лайк, обновляет вес тегов пользователя
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpPost("{id:int}/like")]
    public async Task<IActionResult> Like(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await articleService.LikeAsync(id, userId);
        return NoContent();
    }

    // ? Delete : soft delete статьи — устанавливает DeletedAt
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await articleService.SoftDeleteAsync(id);
        return result ? NoContent() : NotFound();
    }
}
