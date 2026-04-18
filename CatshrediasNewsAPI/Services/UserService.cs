using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Services;

public class UserService(AppDbContext db)
{
    // ? GetAllAsync : возвращает список всех пользователей
    // вызывается из AdminController.GetUsers (Admin)
    public async Task<List<UserDto>> GetAllAsync()
    {
        return await db.Users
            .Include(u => u.Role)
            .Select(u => new UserDto(u.Id, u.Username, u.Email, u.Role.Name, u.IsBlocked))
            .ToListAsync();
    }

    // ? SetBlockedAsync : блокирует или разблокирует пользователя
    // вызывается из AdminController.BlockUser / UnblockUser (Admin)
    public async Task<bool> SetBlockedAsync(int userId, bool blocked)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null) return false;
        user.IsBlocked = blocked;
        await db.SaveChangesAsync();
        return true;
    }

    // ? GetByIdAsync : возвращает профиль пользователя по идентификатору
    // вызывается из UsersController.GetById (Public)
    public async Task<UserDto?> GetByIdAsync(int id)
    {
        return await db.Users
            .Include(u => u.Role)
            .Where(u => u.Id == id)
            .Select(u => new UserDto(u.Id, u.Username, u.Email, u.Role.Name, u.IsBlocked))
            .FirstOrDefaultAsync();
    }

    // ? UpdateAsync : обновляет username, email или пароль текущего пользователя
    // вызывается из UsersController.Update (Auth)
    public async Task<UserDto?> UpdateAsync(int userId, UpdateProfileDto dto)
    {
        var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Username))
            user.Username = dto.Username;

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            if (await db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != userId))
                return null;
            user.Email = dto.Email;
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        await db.SaveChangesAsync();
        return new UserDto(user.Id, user.Username, user.Email, user.Role.Name, user.IsBlocked);
    }

    // ? DeleteAsync : удаляет аккаунт пользователя
    // вызывается из UsersController.Delete (Auth)
    public async Task<bool> DeleteAsync(int userId)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null) return false;
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return true;
    }
}
