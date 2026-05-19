namespace CatshrediasNewsAPI.DTOs;

public record ModerationNoteDto(string Excerpt, string Reason);

public record RejectArticleDto(string? Reason, List<ModerationNoteDto>? Notes);

public record ReportDto(
    int Id,
    string ReportType,
    string? Description,
    string ReportedBy,
    DateTime CreatedAt,
    int ArticleId
);

public record CreateReportDto(int ReportTypeId, string? Description);

public record ModerationLogDto(
    int Id,
    string Action,
    string? Reason,
    string Moderator,
    DateTime CreatedAt,
    int ArticleId
);
