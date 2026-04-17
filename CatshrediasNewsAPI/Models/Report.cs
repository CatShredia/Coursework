namespace CatshrediasNewsAPI.Models;

public class Report
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public int ReportTypeId { get; set; }
    public ReportType ReportType { get; set; } = null!;
}
