using CatshrediasNewsAPI.Models;
using CatshrediasNewsAPI.Services;

namespace CatshrediasNewsAPI.Tests;

public class UserAnonymizationTests
{
    [Fact]
    public void Apply_WithMarkDeleted_SetsDeletedAtAndAnonymizesFields()
    {
        var user = new User
        {
            Id = 42,
            Username = "alice",
            Email = "alice@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret"),
            EmailConfirmed = true,
            EmailConfirmToken = "token",
            EmailConfirmTokenExpiry = DateTime.UtcNow.AddHours(1),
            PasswordResetToken = "reset",
            PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1),
            AvatarUrl = "/uploads/a.png",
            IsBlocked = true
        };

        UserAnonymization.Apply(user);

        Assert.NotNull(user.DeletedAt);
        Assert.StartsWith("deleted_42_", user.Email);
        Assert.EndsWith("@deleted.local", user.Email);
        Assert.StartsWith("deleted_42_", user.Username);
        Assert.False(user.EmailConfirmed);
        Assert.Null(user.EmailConfirmToken);
        Assert.Null(user.EmailConfirmTokenExpiry);
        Assert.Null(user.PasswordResetToken);
        Assert.Null(user.PasswordResetTokenExpiry);
        Assert.Null(user.AvatarUrl);
        Assert.False(user.IsBlocked);
        Assert.NotEqual("secret", user.PasswordHash);
    }

    [Fact]
    public void Apply_WithoutMarkDeleted_KeepsDeletedAtNull()
    {
        var user = new User
        {
            Id = 7,
            Username = "bob",
            Email = "bob@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pw")
        };

        UserAnonymization.Apply(user, markDeleted: false);

        Assert.Null(user.DeletedAt);
        Assert.Contains("deleted_7_", user.Email);
    }
}
