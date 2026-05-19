namespace CatshrediasNewsAPI.Models;

public class ModerationNote
{
    public int Id { get; set; }
    public string Excerpt { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public int SortOrder { get; set; }

    public int ModerationLogId { get; set; }
    public ModerationLog ModerationLog { get; set; } = null!;
}
