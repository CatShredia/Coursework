namespace CatshrediasNewsAPI.Models;

public class RssSource
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Url { get; set; } = null!;
    public bool IsTrusted { get; set; }
    public DateTime? LastFetchedAt { get; set; }

    public ICollection<Article> Articles { get; set; } = [];
}
