using CatshrediasNewsAPI.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace CatshrediasNewsAPI.Hubs;

public class CommentsHub : Hub
{
    // ? JoinArticle : подключает клиента к группе комментариев конкретной статьи
    // вызывается клиентом при открытии страницы статьи
    public async Task JoinArticle(int articleId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"article_{articleId}");
    }

    // ? LeaveArticle : отключает клиента от группы комментариев статьи
    // вызывается клиентом при закрытии страницы статьи
    public async Task LeaveArticle(int articleId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"article_{articleId}");
    }

    // ? SendCommentToGroup : отправляет новый комментарий всем подключённым к статье
    // вызывается из CommentService.CreateAsync после сохранения в БД
    public async Task SendCommentToGroup(int articleId, CommentDto comment)
    {
        await Clients.Group($"article_{articleId}").SendAsync("ReceiveComment", comment);
    }

    // ? SendCommentDeleted : уведомляет о удалении комментария
    // вызывается из CommentService.DeleteAsync после удаления из БД
    public async Task SendCommentDeleted(int articleId, int commentId)
    {
        await Clients.Group($"article_{articleId}").SendAsync("CommentDeleted", commentId);
    }
}
