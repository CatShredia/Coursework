namespace CatshrediasNewsAPI.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = null!; // Admin, Moderator, User

    public ICollection<User> Users { get; set; } = [];
}
