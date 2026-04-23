namespace CatshrediasNewsAPI.DTOs;

public record CommentDto(
    int Id,
    string Content,
    string Username,
    int AuthorId,
    DateTime CreatedAt,
    int? ParentCommentId,
    List<CommentDto> Replies
);

public record CreateCommentDto(string Content, int? ParentCommentId);
