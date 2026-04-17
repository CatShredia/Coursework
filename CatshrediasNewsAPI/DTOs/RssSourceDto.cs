namespace CatshrediasNewsAPI.DTOs;

public record RssSourceDto(int Id, string Name, string Url, bool IsTrusted, DateTime? LastFetchedAt);

public record CreateRssSourceDto(string Name, string Url, bool IsTrusted);

public record UpdateRssSourceDto(string Name, string Url, bool IsTrusted);
