namespace CatshrediasNewsAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public bool IsBlocked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public ICollection<UserTagWeight> TagWeights { get; set; } = [];
    public ICollection<Like> Likes { get; set; } = [];
    public ICollection<SavedArticle> SavedArticles { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Report> Reports { get; set; } = [];
    public ICollection<ModerationLog> ModerationLogs { get; set; } = [];
}
