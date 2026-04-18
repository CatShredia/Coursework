using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatshrediasNewsAPI.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(RssSourceService rssSourceService, TagService tagService, RssFetcherService rssFetcher, UserService userService) : ControllerBase
{
    // ? GetSources : возвращает список всех RSS-источников
    // вызывается клиентом (Admin)
    [HttpGet("sources")]
    public async Task<IActionResult> GetSources()
    {
        return Ok(await rssSourceService.GetAllAsync());
    }

    // ? CreateSource : добавляет новый RSS-источник
    // вызывается клиентом (Admin)
    [HttpPost("sources")]
    public async Task<IActionResult> CreateSource(CreateRssSourceDto dto)
    {
        return Ok(await rssSourceService.CreateAsync(dto));
    }

    // ? UpdateSource : обновляет данные RSS-источника
    // вызывается клиентом (Admin)
    [HttpPut("sources/{id:int}")]
    public async Task<IActionResult> UpdateSource(int id, UpdateRssSourceDto dto)
    {
        var result = await rssSourceService.UpdateAsync(id, dto);
        return result ? NoContent() : NotFound();
    }

    // ? DeleteSource : удаляет RSS-источник
    // вызывается клиентом (Admin)
    [HttpDelete("sources/{id:int}")]
    public async Task<IActionResult> DeleteSource(int id)
    {
        var result = await rssSourceService.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }

    // ? EnableSource : включает RSS-источник
    // вызывается клиентом (Admin)
    [HttpPost("sources/{id:int}/enable")]
    public async Task<IActionResult> EnableSource(int id)
    {
        var result = await rssSourceService.SetEnabledAsync(id, true);
        return result ? NoContent() : NotFound();
    }

    // ? DisableSource : отключает RSS-источник
    // вызывается клиентом (Admin)
    [HttpPost("sources/{id:int}/disable")]
    public async Task<IActionResult> DisableSource(int id)
    {
        var result = await rssSourceService.SetEnabledAsync(id, false);
        return result ? NoContent() : NotFound();
    }

    // ? TriggerRss : принудительно запускает парсинг всех включённых источников прямо сейчас
    // вызывается клиентом (Admin)
    [HttpPost("rss/trigger")]
    public IActionResult TriggerRss()
    {
        rssFetcher.TriggerNow();
        return Ok(new { message = "Парсинг RSS запущен принудительно." });
    }

    // ? SetRssInterval : изменяет интервал автоматического парсинга RSS
    // вызывается клиентом (Admin)
    [HttpPut("rss/interval")]
    public IActionResult SetRssInterval(SetIntervalDto dto)
    {
        if (dto.IntervalMinutes < 1)
            return BadRequest("Интервал должен быть не менее 1 минуты.");
        rssFetcher.SetInterval(dto.IntervalMinutes);
        return Ok(new { message = $"Интервал изменён на {dto.IntervalMinutes} мин.", intervalMinutes = dto.IntervalMinutes });
    }

    // ? GetRssStatus : возвращает текущий интервал парсинга
    // вызывается клиентом (Admin)
    [HttpGet("rss/status")]
    public IActionResult GetRssStatus()
    {
        return Ok(new { intervalMinutes = (int)rssFetcher.CurrentInterval.TotalMinutes });
    }

    // ? GetUsers : возвращает список всех пользователей
    // вызывается клиентом (Admin)
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        return Ok(await userService.GetAllAsync());
    }

    // ? BlockUser : блокирует пользователя
    // вызывается клиентом (Admin)
    [HttpPost("users/{id:int}/block")]
    public async Task<IActionResult> BlockUser(int id)
    {
        var result = await userService.SetBlockedAsync(id, true);
        return result ? NoContent() : NotFound();
    }

    // ? UnblockUser : разблокирует пользователя
    // вызывается клиентом (Admin)
    [HttpPost("users/{id:int}/unblock")]
    public async Task<IActionResult> UnblockUser(int id)
    {
        var result = await userService.SetBlockedAsync(id, false);
        return result ? NoContent() : NotFound();
    }

    // ? DeleteUser : удаляет пользователя по Id
    // вызывается клиентом (Admin)
    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await userService.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }

    // ? GetTags : возвращает список всех тегов
    // вызывается клиентом (Admin)
    [HttpGet("tags")]
    public async Task<IActionResult> GetTags()
    {
        return Ok(await tagService.GetAllAsync());
    }

    // ? CreateTag : создаёт новый тег
    // вызывается клиентом (Admin)
    [HttpPost("tags")]
    public async Task<IActionResult> CreateTag(CreateTagDto dto)
    {
        return Ok(await tagService.CreateAsync(dto));
    }

    // ? DeleteTag : удаляет тег по идентификатору
    // вызывается клиентом (Admin)
    [HttpDelete("tags/{id:int}")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var result = await tagService.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }
}
