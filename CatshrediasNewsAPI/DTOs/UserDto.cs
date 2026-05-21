namespace CatshrediasNewsAPI.DTOs;

public record UserDto(int Id, string Username, string Email, string Role, bool IsBlocked, string? AvatarUrl, string AvatarColor, bool PersonalizedFeedEnabled = true);

public record SetPersonalizedFeedDto(bool Enabled);

public record RegisterDto(
    string Username,
    string Email,
    string Password,
    string AvatarColor = "#1a73e8",
    string? AvatarDataUrl = null,
    string? Culture = null);

public record LoginDto(string Email, string Password);

public record AuthResponseDto(string Token, UserDto User);

public record UpdateProfileDto(string? Username, string? Email, string? Password, string? AvatarColor);

public record ResetPasswordDto(string Token, string NewPassword);
