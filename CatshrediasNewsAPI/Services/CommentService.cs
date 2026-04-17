using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Services;

public class CommentService(AppDbContext db)
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

    // ? CreateAsync : создаёт новый комментарий к статье
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
        return new CommentDto(comment.Id, comment.Content, comment.User.Username, comment.CreatedAt, comment.ParentCommentId, []);
    }

    // ? DeleteAsync : удаляет комментарий (только автор или модератор)
    // вызывается из CommentsController.Delete (Auth)
    public async Task<bool> DeleteAsync(int commentId, int userId, bool isModerator)
    {
        var comment = await db.Comments.FindAsync(commentId);
        if (comment is null) return false;
        if (!isModerator && comment.UserId != userId) return false;
        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
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
