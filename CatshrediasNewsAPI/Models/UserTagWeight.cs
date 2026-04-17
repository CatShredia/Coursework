namespace CatshrediasNewsAPI.Models;

public class UserTagWeight
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;

    public float Weight { get; set; } = 1.0f;
    public bool IsSubscribed { get; set; }
}
