namespace CatshrediasNews.Client.Models;

public class ArticleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string? ContentHtml { get; set; }
    public string? ImageUrl { get; set; }
    public string? RssAuthor { get; set; }
    public string? SourceUrl { get; set; }
    public DateTime PublishedAt { get; set; }
    public string Status { get; set; } = "";
    public int? AuthorId { get; set; }
    public string? Author { get; set; }
    public List<string> Tags { get; set; } = [];
    public int LikesCount { get; set; }
    public string? SourceName { get; set; }
}

public class TagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public enum FeedSort { Newest, Oldest, Popular }

public class ModerationCounts
{
    public int Queue   { get; set; }
    public int Reports { get; set; }
}

public class ReportDto
{
    public int      Id          { get; set; }
    public string   ReportType  { get; set; } = "";
    public string?  Description { get; set; }
    public string   ReportedBy  { get; set; } = "";
    public DateTime CreatedAt   { get; set; }
    public int      ArticleId   { get; set; }
}
