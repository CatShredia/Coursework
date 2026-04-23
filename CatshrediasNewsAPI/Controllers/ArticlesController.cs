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
    // ? Search : поиск статей по заголовку и тексту
    // вызывается клиентом (Public)
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] string? source = null)
    {
        return Ok(await articleService.SearchAsync(q, source));
    }

    // ? GetSaved : возвращает сохранённые статьи пользователя
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpGet("saved")]
    public async Task<IActionResult> GetSaved()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await articleService.GetSavedAsync(userId));
    }

    // ? ToggleSave : сохраняет или удаляет статью из избранного
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpPost("{id:int}/save")]
    public async Task<IActionResult> ToggleSave(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var saved  = await articleService.ToggleSaveAsync(id, userId);
        return Ok(new { saved });
    }

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

    // ? SaveDraft : сохраняет статью как черновик в БД
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpPost("draft")]
    public async Task<IActionResult> SaveDraft([FromBody] SaveDraftRequest req)
    {
        var userId  = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dto     = new CreateArticleDto(req.Title, req.Content, null, req.ImageUrl, DateTime.UtcNow, req.TagIds);
        var article = await articleService.SaveDraftAsync(userId, req.ArticleId, dto);
        return Ok(article);
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

    // ? GetLikedIds : возвращает Id всех лайкнутых статей пользователя
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpGet("liked-ids")]
    public async Task<IActionResult> GetLikedIds()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await articleService.GetLikedIdsAsync(userId));
    }

    // ? GetSavedIds : возвращает Id всех сохранённых статей пользователя
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpGet("saved-ids")]
    public async Task<IActionResult> GetSavedIds()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await articleService.GetSavedIdsAsync(userId));
    }

    // ? IsLiked : проверяет, лайкнул ли текущий пользователь статью
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpGet("{id:int}/liked")]
    public async Task<IActionResult> IsLiked(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await articleService.IsLikedAsync(id, userId));
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

    // ? Update : обновляет статью автора
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateArticleDto dto)
    {
        var userId  = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var article = await articleService.UpdateAsync(id, userId, dto);
        return article is null ? NotFound() : Ok(article);
    }

    // ? UploadImage : загружает изображение для статьи и возвращает URL
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage(IFormFile upload,
        [FromServices] IWebHostEnvironment env)
    {
        if (upload is null || upload.Length == 0) return BadRequest("Файл не выбран.");
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        if (!allowed.Contains(Path.GetExtension(upload.FileName).ToLowerInvariant()))
            return BadRequest("Допустимые форматы: jpg, png, webp, gif.");
        if (upload.Length > 10 * 1024 * 1024) return BadRequest("Файл не должен превышать 10 МБ.");

        var dir      = Path.Combine(env.ContentRootPath, "uploads", "articles");
        Directory.CreateDirectory(dir);
        var ext      = Path.GetExtension(upload.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{ext}";
        await using var stream = System.IO.File.Create(Path.Combine(dir, fileName));
        await upload.CopyToAsync(stream);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(new { url = $"{baseUrl}/uploads/articles/{fileName}" });
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
