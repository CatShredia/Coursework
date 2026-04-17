using CatshrediasNewsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PublicationStatus> PublicationStatuses => Set<PublicationStatus>();
    public DbSet<RssSource> RssSources => Set<RssSource>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();
    public DbSet<UserTagWeight> UserTagWeights => Set<UserTagWeight>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<SavedArticle> SavedArticles => Set<SavedArticle>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ReportType> ReportTypes => Set<ReportType>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ModerationLog> ModerationLogs => Set<ModerationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<ArticleTag>()
            .HasKey(at => new { at.ArticleId, at.TagId });

        modelBuilder.Entity<UserTagWeight>()
            .HasKey(utw => new { utw.UserId, utw.TagId });

        modelBuilder.Entity<Like>()
            .HasKey(l => new { l.UserId, l.ArticleId });

        modelBuilder.Entity<SavedArticle>()
            .HasKey(sa => new { sa.UserId, sa.ArticleId });

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ModerationLog>()
            .HasOne(ml => ml.Moderator)
            .WithMany(u => u.ModerationLogs)
            .HasForeignKey(ml => ml.ModeratorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PublicationStatus>().HasData(
            new PublicationStatus { Id = 1, Name = "Draft" },
            new PublicationStatus { Id = 2, Name = "PendingReview" },
            new PublicationStatus { Id = 3, Name = "Published" },
            new PublicationStatus { Id = 4, Name = "Rejected" }
        );

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "Moderator" },
            new Role { Id = 3, Name = "User" }
        );

        modelBuilder.Entity<ReportType>().HasData(
            new ReportType { Id = 1, Name = "Spam" },
            new ReportType { Id = 2, Name = "Hate" },
            new ReportType { Id = 3, Name = "Fake" }
        );
    }
}
