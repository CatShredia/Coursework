using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatshrediasNewsAPI.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(RssSourceService rssSourceService, TagService tagService) : ControllerBase
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
