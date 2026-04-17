namespace CatshrediasNewsAPI.Models;

public class ReportType
{
    public int Id { get; set; }
    public string Name { get; set; } = null!; // Spam, Hate, Fake

    public ICollection<Report> Reports { get; set; } = [];
}
