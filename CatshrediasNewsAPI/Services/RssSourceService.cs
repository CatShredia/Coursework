using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Services;

public class RssSourceService(AppDbContext db)
{
    // ? GetAllAsync : возвращает список всех RSS-источников
    // вызывается из RssSourcesController.GetAll (Admin)
    public async Task<List<RssSourceDto>> GetAllAsync()
    {
        return await db.RssSources
            .Select(s => new RssSourceDto(s.Id, s.Name, s.Url, s.IsTrusted, s.LastFetchedAt))
            .ToListAsync();
    }

    // ? CreateAsync : добавляет новый RSS-источник
    // вызывается из RssSourcesController.Create (Admin)
    public async Task<RssSourceDto> CreateAsync(CreateRssSourceDto dto)
    {
        var source = new RssSource { Name = dto.Name, Url = dto.Url, IsTrusted = dto.IsTrusted };
        db.RssSources.Add(source);
        await db.SaveChangesAsync();
        return new RssSourceDto(source.Id, source.Name, source.Url, source.IsTrusted, source.LastFetchedAt);
    }

    // ? UpdateAsync : обновляет данные RSS-источника
    // вызывается из RssSourcesController.Update (Admin)
    public async Task<bool> UpdateAsync(int id, UpdateRssSourceDto dto)
    {
        var source = await db.RssSources.FindAsync(id);
        if (source is null) return false;
        source.Name = dto.Name;
        source.Url = dto.Url;
        source.IsTrusted = dto.IsTrusted;
        await db.SaveChangesAsync();
        return true;
    }

    // ? DeleteAsync : удаляет RSS-источник по идентификатору
    // вызывается из RssSourcesController.Delete (Admin)
    public async Task<bool> DeleteAsync(int id)
    {
        var source = await db.RssSources.FindAsync(id);
        if (source is null) return false;
        db.RssSources.Remove(source);
        await db.SaveChangesAsync();
        return true;
    }
}
