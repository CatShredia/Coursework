using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Services;

public class TagService(AppDbContext db)
{
    // ? GetAllAsync : возвращает список всех тегов
    // вызывается из TagsController.GetAll
    public async Task<List<TagDto>> GetAllAsync()
    {
        return await db.Tags
            .Select(t => new TagDto(t.Id, t.Name))
            .ToListAsync();
    }

    // ? CreateAsync : создаёт новый тег
    // вызывается из TagsController.Create (Admin)
    public async Task<TagDto> CreateAsync(CreateTagDto dto)
    {
        var tag = new Tag { Name = dto.Name };
        db.Tags.Add(tag);
        await db.SaveChangesAsync();
        return new TagDto(tag.Id, tag.Name);
    }

    // ? DeleteAsync : удаляет тег по идентификатору
    // вызывается из TagsController.Delete (Admin)
    public async Task<bool> DeleteAsync(int id)
    {
        var tag = await db.Tags.FindAsync(id);
        if (tag is null) return false;
        db.Tags.Remove(tag);
        await db.SaveChangesAsync();
        return true;
    }

    // ? UpdateSubscriptionsAsync : обновляет подписки пользователя на теги
    // вызывается из TagsController.UpdateSubscriptions (Auth)
    public async Task UpdateSubscriptionsAsync(int userId, UpdateTagSubscriptionsDto dto)
    {
        var existing = await db.UserTagWeights
            .Where(utw => utw.UserId == userId)
            .ToListAsync();

        foreach (var utw in existing)
            utw.IsSubscribed = dto.TagIds.Contains(utw.TagId);

        var existingTagIds = existing.Select(utw => utw.TagId).ToHashSet();
        var toAdd = dto.TagIds
            .Where(tagId => !existingTagIds.Contains(tagId))
            .Select(tagId => new UserTagWeight { UserId = userId, TagId = tagId, IsSubscribed = true });

        db.UserTagWeights.AddRange(toAdd);
        await db.SaveChangesAsync();
    }
}
