using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatshrediasNewsAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    // ? Register : регистрирует нового пользователя и возвращает JWT-токен
    // вызывается клиентом (Public)
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var result = await authService.RegisterAsync(dto);
        if (result is null)
            return Conflict("Пользователь с таким email уже существует.");
        return Ok(result);
    }

    // ? Login : выполняет вход и возвращает JWT-токен
    // вызывается клиентом (Public)
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await authService.LoginAsync(dto);
        if (result is null)
            return Unauthorized("Неверный email или пароль.");
        return Ok(result);
    }
}
