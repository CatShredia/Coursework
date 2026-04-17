namespace CatshrediasNewsAPI.Models;

public class PublicationStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = null!; // Draft, PendingReview, Published, Rejected

    public ICollection<Article> Articles { get; set; } = [];
}
