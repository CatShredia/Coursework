using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CatshrediasNewsAPI.Services;

public class AuthService(
    AppDbContext db,
    IConfiguration config,
    EmailService emailService,
    IWebHostEnvironment env,
    IHttpContextAccessor httpContextAccessor)
{
    public const string PasswordVersionClaim = "pwdv";
    private string UploadsRoot => Path.Combine(env.ContentRootPath, "uploads");

    // ? RegisterAsync : регистрирует нового пользователя с ролью User
    // вызывается из AuthController.Register (Public)
    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        if (await db.Users.AnyAsync(u => u.Email == dto.Email))
            return null;

        var userRole = await db.Roles.FirstAsync(r => r.Name == "User");

        var user = new User
        {
            Username     = dto.Username,
            Email        = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId       = userRole.Id,
            AvatarColor  = string.IsNullOrWhiteSpace(dto.AvatarColor) ? "#1a73e8" : dto.AvatarColor,
            AvatarUrl    = SaveAvatarFromDataUrl(dto.AvatarDataUrl),
            EmailConfirmed          = false,
            EmailConfirmToken       = Guid.NewGuid().ToString("N"),
            EmailConfirmTokenExpiry = DateTime.UtcNow.AddHours(24)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        await db.Entry(user).Reference(u => u.Role).LoadAsync();

        await emailService.SendConfirmationAsync(user.Email, user.Username, user.EmailConfirmToken!);

        return new AuthResponseDto(GenerateToken(user), MapToDto(user));
    }

    // ? LoginAsync : проверяет учётные данные и возвращает JWT-токен
    // вызывается из AuthController.Login (Public)
    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return null;

        if (user.IsBlocked)
            return null;

        if (!user.EmailConfirmed)
            return null; // клиент получит 401 и покажет нужное сообщение

        return new AuthResponseDto(GenerateToken(user), MapToDto(user));
    }

    // ? GenerateToken : формирует JWT-токен с клеймами пользователя
    // вызывается из RegisterAsync, LoginAsync
    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.Name),
            new Claim(PasswordVersionClaim, ComputePasswordVersion(user.PasswordHash))
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string ComputePasswordVersion(string passwordHash)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash));
        return Convert.ToHexString(bytes);
    }

    private static UserDto MapToDto(User u) => new(u.Id, u.Username, u.Email, u.Role.Name, u.IsBlocked, u.AvatarUrl, u.AvatarColor);

    private string? SaveAvatarFromDataUrl(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl))
            return null;

        const string marker = ";base64,";
        var markerIndex = dataUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (!dataUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) || markerIndex < 0)
            return null;

        var mime = dataUrl[5..markerIndex];
        var ext = mime switch
        {
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => null
        };

        if (ext is null)
            return null;

        var base64 = dataUrl[(markerIndex + marker.Length)..];
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64);
        }
        catch
        {
            return null;
        }

        if (bytes.Length == 0 || bytes.Length > 5 * 1024 * 1024)
            return null;

        var avatarsDir = Path.Combine(UploadsRoot, "avatars");
        Directory.CreateDirectory(avatarsDir);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(avatarsDir, fileName);
        File.WriteAllBytes(filePath, bytes);
        var request = httpContextAccessor.HttpContext?.Request;
        var apiBaseUrl = request is null
            ? null
            : $"{request.Scheme}://{request.Host}".TrimEnd('/');

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            apiBaseUrl = config["Api:BaseUrl"]?.TrimEnd('/');

        return string.IsNullOrWhiteSpace(apiBaseUrl)
            ? $"/uploads/avatars/{fileName}"
            : $"{apiBaseUrl}/uploads/avatars/{fileName}";
    }

    // ? IsEmailConfirmedAsync : проверяет, подтверждён ли email у пользователя с правильным паролем
    // возвращает null если пользователь не найден / пароль неверен
    public async Task<bool?> IsEmailConfirmedAsync(LoginDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return null;
        return user.EmailConfirmed;
    }

    // ? ConfirmEmailAsync : подтверждает email по токену
    // вызывается из AuthController.ConfirmEmail (Public)
    public async Task<bool> ConfirmEmailAsync(string token)
    {
        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.EmailConfirmToken == token &&
            u.EmailConfirmTokenExpiry > DateTime.UtcNow);

        if (user is null) return false;

        user.EmailConfirmed          = true;
        user.EmailConfirmToken       = null;
        user.EmailConfirmTokenExpiry = null;
        await db.SaveChangesAsync();
        return true;
    }

    // ? SendPasswordResetAsync : генерирует токен сброса и отправляет письмо
    // вызывается из AuthController.ForgotPassword (Public)
    public async Task SendPasswordResetAsync(string email)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) return; // не раскрываем, существует ли email

        user.PasswordResetToken       = Guid.NewGuid().ToString("N");
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await db.SaveChangesAsync();

        await emailService.SendPasswordResetAsync(user.Email, user.Username, user.PasswordResetToken!);
    }

    // ? ResetPasswordAsync : проверяет токен и устанавливает новый пароль
    // вызывается из AuthController.ResetPassword (Public)
    public async Task<(bool ok, string? error)> ResetPasswordAsync(string token, string newPassword)
    {
        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == token &&
            u.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user is null) return (false, null);

        if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
            return (false, "same_password");

        user.PasswordHash             = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordResetToken       = null;
        user.PasswordResetTokenExpiry = null;
        await db.SaveChangesAsync();
        return (true, null);
    }

    // ? EmailExistsAsync : проверяет, зарегистрирован ли email в системе
    // вызывается из AuthController.CheckEmail (Public)
    public async Task<bool> EmailExistsAsync(string email) =>
        await db.Users.AnyAsync(u => u.Email == email);
}
