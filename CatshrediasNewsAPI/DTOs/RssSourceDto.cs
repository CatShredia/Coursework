namespace CatshrediasNewsAPI.DTOs;

public record RssSourceDto(
    int Id, string Name, string Url, bool IsTrusted, bool IsEnabled, DateTime? LastFetchedAt,
    string SourceType,
    string? LinkSelector, string? TitleSelector, string? ContentSelector,
    string? DateSelector, string? ImageSelector);

public record CreateRssSourceDto(
    string Name, string Url, bool IsTrusted,
    string SourceType = "Rss",
    string? LinkSelector = null, string? TitleSelector = null, string? ContentSelector = null,
    string? DateSelector = null, string? ImageSelector = null);

public record UpdateRssSourceDto(
    string Name, string Url, bool IsTrusted,
    string SourceType = "Rss",
    string? LinkSelector = null, string? TitleSelector = null, string? ContentSelector = null,
    string? DateSelector = null, string? ImageSelector = null);

public record SetIntervalDto(int IntervalMinutes);
