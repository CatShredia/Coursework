namespace CatshrediasNews.Client.Services;

public class ArticleHeadingsService
{
    public record Heading(string Level, string Text, string Anchor);

    public IReadOnlyList<Heading> Headings { get; private set; } = [];

    public event Action? OnChange;

    public void Set(IReadOnlyList<Heading> headings)
    {
        Headings = headings;
        OnChange?.Invoke();
    }

    public void Clear()
    {
        Headings = [];
        OnChange?.Invoke();
    }
}
