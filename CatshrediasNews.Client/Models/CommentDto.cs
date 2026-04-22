namespace CatshrediasNews.Client.Models;

public class CommentDto
{
    public int           Id              { get; set; }
    public string        Content         { get; set; } = "";
    public string        Username        { get; set; } = "";
    public int?          AuthorId        { get; set; }
    public DateTime      CreatedAt       { get; set; }
    public int?          ParentCommentId { get; set; }
    public List<CommentDto> Replies      { get; set; } = [];
}
