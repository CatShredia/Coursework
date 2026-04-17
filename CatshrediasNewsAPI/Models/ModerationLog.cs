namespace CatshrediasNewsAPI.Models;

public class ModerationLog
{
    public int Id { get; set; }
    public string Action { get; set; } = null!;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ModeratorId { get; set; }
    public User Moderator { get; set; } = null!;

    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
}
