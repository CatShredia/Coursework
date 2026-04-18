namespace CatshrediasNewsAPI.DTOs;

public record ArticleDto(
    int Id,
    string Title,
    string Content,
    string? SourceUrl,
    DateTime PublishedAt,
    string Status,
    string? Author,
    List<string> Tags,
    int LikesCount,
    string? SourceName
);

public record CreateArticleDto(
    string Title,
    string Content,
    string? SourceUrl,
    DateTime PublishedAt,
    List<int> TagIds
);

public record UpdateArticleDto(
    string Title,
    string Content,
    string? SourceUrl,
    List<int> TagIds
);
