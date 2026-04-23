namespace CatshrediasNews.Client.Models;

public class UserInfo
{
    public int    Id          { get; set; }
    public string Username    { get; set; } = "";
    public string Email       { get; set; } = "";
    public string Role        { get; set; } = "";
    public bool   IsBlocked   { get; set; }
    public string? AvatarUrl  { get; set; }
    public string AvatarColor { get; set; } = "#1a73e8";
}

public class RssSourceDto
{
    public int       Id               { get; set; }
    public string    Name             { get; set; } = "";
    public string    Url              { get; set; } = "";
    public bool      IsTrusted        { get; set; }
    public bool      IsEnabled        { get; set; }
    public DateTime? LastFetchedAt    { get; set; }
    public string    SourceType       { get; set; } = "Rss";
    public string?   LinkSelector     { get; set; }
    public string?   TitleSelector    { get; set; }
    public string?   ContentSelector  { get; set; }
    public string?   DateSelector     { get; set; }
    public string?   ImageSelector    { get; set; }
}

public class RssStatusDto
{
    public int IntervalMinutes { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = "";
    public UserInfo User { get; set; } = new();
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterRequest
{
    public string Username    { get; set; } = "";
    public string Email       { get; set; } = "";
    public string Password    { get; set; } = "";
    public string AvatarColor { get; set; } = "#1a73e8";
    public string? AvatarDataUrl { get; set; }
}
