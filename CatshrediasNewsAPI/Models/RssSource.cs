namespace CatshrediasNewsAPI.Models;

public enum SourceType { Rss, Scraper }

public class RssSource
{
    public int         Id             { get; set; }
    public string      Name           { get; set; } = null!;
    public string      Url            { get; set; } = null!;
    public bool        IsTrusted      { get; set; }
    public bool        IsEnabled      { get; set; } = true;
    public DateTime?   LastFetchedAt  { get; set; }
    public SourceType  SourceType     { get; set; } = SourceType.Rss;

    // Селекторы для скрапера (null если SourceType == Rss)
    public string? LinkSelector       { get; set; } // Селектор ссылок на статьи
    public string? TitleSelector      { get; set; } // Селектор заголовка
    public string? ContentSelector    { get; set; } // Селектор текста
    public string? DateSelector       { get; set; } // Селектор даты
    public string? ImageSelector      { get; set; } // Селектор изображения

    public ICollection<Article> Articles { get; set; } = [];
}
