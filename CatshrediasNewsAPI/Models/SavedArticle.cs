namespace CatshrediasNewsAPI.Models;

public class SavedArticle
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
