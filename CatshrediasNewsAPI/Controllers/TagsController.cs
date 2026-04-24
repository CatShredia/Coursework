using System.Security.Claims;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatshrediasNewsAPI.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController(TagService tagService) : ControllerBase
{
    // ? GetAll : возвращает список всех тегов
    // вызывается клиентом (Public)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await tagService.GetAllAsync());
    }

    // ? Create : создаёт новый тег
    // вызывается клиентом (Admin)
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateTagDto dto)
    {
        return Ok(await tagService.CreateAsync(dto));
    }

    // ? Delete : удаляет тег по идентификатору
    // вызывается клиентом (Admin)
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await tagService.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }

    // ? UpdateSubscriptions : обновляет подписки текущего пользователя на теги
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpPut("subscriptions")]
    public async Task<IActionResult> UpdateSubscriptions(UpdateTagSubscriptionsDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await tagService.UpdateSubscriptionsAsync(userId, dto);
        return NoContent();
    }
}
