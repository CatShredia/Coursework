namespace CatshrediasNewsAPI.Models;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = null!; // IT, Sport, Politics...

    public ICollection<ArticleTag> ArticleTags { get; set; } = [];
    public ICollection<UserTagWeight> UserTagWeights { get; set; } = [];
}
