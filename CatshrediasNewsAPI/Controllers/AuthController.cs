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
        // Проверяем отдельно: есть ли пользователь с таким email и правильным паролем, но не подтверждён
        var emailConfirmed = await authService.IsEmailConfirmedAsync(dto);
        if (emailConfirmed == false)
            return Unauthorized("email_not_confirmed");

        var result = await authService.LoginAsync(dto);
        if (result is null)
            return Unauthorized("Неверный email или пароль.");
        return Ok(result);
    }

    // ? ForgotPassword : отправляет письмо со ссылкой сброса пароля
    // вызывается клиентом (Public)
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] string email)
    {
        await authService.SendPasswordResetAsync(email);
        return Ok(); // всегда 200 — не раскрываем, есть ли такой email
    }

    // ? ResetPassword : устанавливает новый пароль по токену
    // вызывается клиентом (Public)
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var (ok, error) = await authService.ResetPasswordAsync(dto.Token, dto.NewPassword);
        if (!ok) return BadRequest(error ?? "invalid_token");
        return Ok();
    }

    // ? CheckEmail : проверяет, зарегистрирован ли email
    // вызывается клиентом перед отправкой запроса сброса (Public)
    [HttpPost("check-email")]
    public async Task<IActionResult> CheckEmail([FromBody] string email)
    {
        var exists = await authService.EmailExistsAsync(email);
        return Ok(exists);
    }
    // ? ConfirmEmail : подтверждает email по токену из письма
    // вызывается по ссылке из письма (Public)
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
    {
        var ok = await authService.ConfirmEmailAsync(token);
        if (!ok) return BadRequest("Ссылка недействительна или устарела.");
        return Ok();
    }
}
