using CatshrediasNewsAPI.Models;

namespace CatshrediasNewsAPI.Services;

/// <summary>
/// Освобождает email/username при удалении аккаунта для повторной регистрации.
/// </summary>
public static class UserAnonymization
{
    public static void Apply(User user, bool markDeleted = true)
    {
        if (markDeleted)
            user.DeletedAt = DateTime.UtcNow;

        var suffix = Guid.NewGuid().ToString("N");
        user.Email = $"deleted_{user.Id}_{suffix}@deleted.local";
        user.Username = $"deleted_{user.Id}_{suffix}";
        user.EmailConfirmed = false;
        user.EmailConfirmToken = null;
        user.EmailConfirmTokenExpiry = null;
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));
        user.IsBlocked = false;
        user.AvatarUrl = null;
    }
}
