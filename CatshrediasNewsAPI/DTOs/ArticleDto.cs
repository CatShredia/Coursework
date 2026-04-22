namespace CatshrediasNewsAPI.DTOs;

public record ArticleDto(
    int Id,
    string Title,
    string Content,
    string? ContentHtml,
    string? ImageUrl,
    string? RssAuthor,
    string? SourceUrl,
    DateTime PublishedAt,
    string Status,
    int? AuthorId,
    string? Author,
    List<string> Tags,
    int LikesCount,
    string? SourceName
);

public record CreateArticleDto(
    string Title,
    string Content,
    string? SourceUrl,
    string? ImageUrl,
    DateTime PublishedAt,
    List<int> TagIds
);

public record UpdateArticleDto(
    string Title,
    string Content,
    string? SourceUrl,
    List<int> TagIds
);

public record SaveDraftRequest(
    int? ArticleId,
    string Title,
    string Content,
    string? ImageUrl,
    List<int> TagIds
);
