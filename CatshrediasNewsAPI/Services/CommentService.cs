using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Hubs;
using CatshrediasNewsAPI.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Services;

public class CommentService(AppDbContext db, IHubContext<CommentsHub> hub)
{
    // ? GetByArticleAsync : возвращает дерево комментариев для статьи
    // вызывается из CommentsController.GetByArticle
    public async Task<List<CommentDto>> GetByArticleAsync(int articleId)
    {
        var all = await db.Comments
            .Where(c => c.ArticleId == articleId)
            .Include(c => c.User)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return BuildTree(all, null);
    }

    // ? CreateAsync : создаёт комментарий и рассылает его через SignalR всем в группе статьи
    // вызывается из CommentsController.Create (Auth)
    public async Task<CommentDto> CreateAsync(int articleId, int userId, CreateCommentDto dto)
    {
        var comment = new Comment
        {
            Content = dto.Content,
            ArticleId = articleId,
            UserId = userId,
            ParentCommentId = dto.ParentCommentId
        };

        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        await db.Entry(comment).Reference(c => c.User).LoadAsync();

        var commentDto = new CommentDto(
            comment.Id, comment.Content, comment.User.Username,
            comment.CreatedAt, comment.ParentCommentId, []);

        await hub.Clients.Group($"article_{articleId}")
            .SendAsync("ReceiveComment", commentDto);

        return commentDto;
    }

    // ? DeleteAsync : удаляет комментарий и уведомляет группу статьи через SignalR
    // вызывается из CommentsController.Delete (Auth)
    public async Task<bool> DeleteAsync(int commentId, int userId, bool isModerator)
    {
        var comment = await db.Comments.FindAsync(commentId);
        if (comment is null) return false;
        if (!isModerator && comment.UserId != userId) return false;

        var articleId = comment.ArticleId;
        db.Comments.Remove(comment);
        await db.SaveChangesAsync();

        await hub.Clients.Group($"article_{articleId}")
            .SendAsync("CommentDeleted", commentId);

        return true;
    }

    private static List<CommentDto> BuildTree(List<Comment> all, int? parentId)
    {
        return all
            .Where(c => c.ParentCommentId == parentId)
            .Select(c => new CommentDto(
                c.Id, c.Content, c.User.Username, c.CreatedAt,
                c.ParentCommentId, BuildTree(all, c.Id)))
            .ToList();
    }
}
