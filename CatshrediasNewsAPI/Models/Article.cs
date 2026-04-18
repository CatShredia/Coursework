namespace CatshrediasNewsAPI.Models;

public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? ContentHtml { get; set; }
    public string? ImageUrl { get; set; }
    public string? RssAuthor { get; set; }
    public string? SourceUrl { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public int StatusId { get; set; }
    public PublicationStatus Status { get; set; } = null!;

    public int? RssSourceId { get; set; }
    public RssSource? RssSource { get; set; }

    public int? AuthorId { get; set; }
    public User? Author { get; set; }

    public ICollection<ArticleTag> ArticleTags { get; set; } = [];
    public ICollection<Like> Likes { get; set; } = [];
    public ICollection<SavedArticle> SavedArticles { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Report> Reports { get; set; } = [];
    public ICollection<ModerationLog> ModerationLogs { get; set; } = [];
}
