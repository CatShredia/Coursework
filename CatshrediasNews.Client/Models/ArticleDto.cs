namespace CatshrediasNews.Client.Models;

public class ArticleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string? SourceUrl { get; set; }
    public DateTime PublishedAt { get; set; }
    public string Status { get; set; } = "";
    public string? Author { get; set; }
    public List<string> Tags { get; set; } = [];
    public int LikesCount { get; set; }
}

public class TagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public enum FeedSort { Newest, Oldest, Popular }
