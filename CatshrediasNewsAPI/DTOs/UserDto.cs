namespace CatshrediasNewsAPI.DTOs;

public record UserDto(int Id, string Username, string Email, string Role, bool IsBlocked);

public record RegisterDto(string Username, string Email, string Password);

public record LoginDto(string Email, string Password);

public record AuthResponseDto(string Token, UserDto User);

public record UpdateProfileDto(string? Username, string? Email, string? Password);
