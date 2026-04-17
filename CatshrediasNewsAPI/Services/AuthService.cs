using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CatshrediasNewsAPI.Services;

public class AuthService(AppDbContext db, IConfiguration config)
{
    // ? RegisterAsync : регистрирует нового пользователя с ролью User
    // вызывается из AuthController.Register (Public)
    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        if (await db.Users.AnyAsync(u => u.Email == dto.Email))
            return null;

        var userRole = await db.Roles.FirstAsync(r => r.Name == "User");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = userRole.Id
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        await db.Entry(user).Reference(u => u.Role).LoadAsync();

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
            new Claim(ClaimTypes.Role, user.Role.Name)
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

    private static UserDto MapToDto(User u) => new(u.Id, u.Username, u.Email, u.Role.Name, u.IsBlocked);
}
