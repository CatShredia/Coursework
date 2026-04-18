using System.Security.Claims;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatshrediasNewsAPI.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(UserService userService) : ControllerBase
{
    // ? GetById : возвращает публичный профиль пользователя по идентификатору
    // вызывается клиентом (Public)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await userService.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    // ? GetMe : возвращает профиль текущего авторизованного пользователя
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await userService.GetByIdAsync(userId);
        return user is null ? NotFound() : Ok(user);
    }

    // ? Update : обновляет username, email или пароль текущего пользователя
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> Update(UpdateProfileDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await userService.UpdateAsync(userId, dto);
        if (result is null)
            return Conflict("Email уже занят другим пользователем.");
        return Ok(result);
    }

    // ? UploadAvatar : загружает фото профиля
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile upload)
    {
        if (upload is null || upload.Length == 0) return BadRequest("Файл не выбран.");
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        if (!allowed.Contains(Path.GetExtension(upload.FileName).ToLowerInvariant()))
            return BadRequest("Допустимые форматы: jpg, png, webp, gif.");
        if (upload.Length > 5 * 1024 * 1024) return BadRequest("Файл не должен превышать 5 МБ.");

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await userService.UploadAvatarAsync(userId, upload, $"{Request.Scheme}://{Request.Host}");
        return result is null ? NotFound() : Ok(result);
    }

    // ? Delete : удаляет аккаунт текущего пользователя
    // вызывается клиентом (Auth)
    [Authorize]
    [HttpDelete("me")]
    public async Task<IActionResult> Delete()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await userService.DeleteAsync(userId);
        return result ? NoContent() : NotFound();
    }
}
