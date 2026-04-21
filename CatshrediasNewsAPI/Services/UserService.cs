using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Services;

public class UserService(AppDbContext db, IWebHostEnvironment env)
{
    private string UploadsRoot => Path.Combine(env.ContentRootPath, "uploads");
    // ? GetAllAsync : возвращает список всех пользователей
    // вызывается из AdminController.GetUsers (Admin)
    public async Task<List<UserDto>> GetAllAsync()
    {
        return await db.Users
            .Include(u => u.Role)
            .Select(u => Map(u))
            .ToListAsync();
    }

    // ? SetRoleAsync : меняет роль пользователя
    // вызывается из AdminController.SetRole (Admin)
    public async Task<bool> SetRoleAsync(int userId, string roleName)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null) return false;
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role is null) return false;
        user.RoleId = role.Id;
        await db.SaveChangesAsync();
        return true;
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
        var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? null : Map(user);
    }

    // ? UpdateAsync : обновляет username, email, пароль или цвет аватара
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

        if (!string.IsNullOrWhiteSpace(dto.AvatarColor))
            user.AvatarColor = dto.AvatarColor;

        await db.SaveChangesAsync();
        return Map(user);
    }

    // ? UploadAvatarAsync : сохраняет загруженный файл аватара и обновляет AvatarUrl
    // вызывается из UsersController.UploadAvatar (Auth)
    public async Task<UserDto?> UploadAvatarAsync(int userId, IFormFile file, string baseUrl)
    {
        var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return null;

        var uploadsDir = Path.Combine(UploadsRoot, "avatars");
        Directory.CreateDirectory(uploadsDir);

        // Удаляем старый файл если был
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            var oldPath = Path.Combine(UploadsRoot, "avatars",
                Path.GetFileName(user.AvatarUrl));
            if (File.Exists(oldPath)) File.Delete(oldPath);
        }

        var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{userId}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream);

        user.AvatarUrl = $"{baseUrl}/uploads/avatars/{fileName}";
        await db.SaveChangesAsync();
        return Map(user);
    }

    // ? DeleteAvatarAsync : удаляет файл аватара и сбрасывает AvatarUrl
    // вызывается из UsersController.DeleteAvatar (Auth)
    public async Task<UserDto?> DeleteAvatarAsync(int userId)
    {
        var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return null;

        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            var oldPath = Path.Combine(UploadsRoot, "avatars", Path.GetFileName(user.AvatarUrl));
            if (File.Exists(oldPath)) File.Delete(oldPath);
            user.AvatarUrl = null;
            await db.SaveChangesAsync();
        }

        return Map(user);
    }

    // ? DeleteAsync : soft delete аккаунта пользователя — устанавливает DeletedAt
    // вызывается из UsersController.Delete (Auth) и AdminController.DeleteUser (Admin)
    public async Task<bool> DeleteAsync(int userId)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null) return false;
        user.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    private static UserDto Map(Models.User u) =>
        new(u.Id, u.Username, u.Email, u.Role.Name, u.IsBlocked, u.AvatarUrl, u.AvatarColor);
}
