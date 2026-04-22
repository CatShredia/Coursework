using CatshrediasNewsAPI.Data;
using CatshrediasNewsAPI.DTOs;
using CatshrediasNewsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Services;

public class RssSourceService(AppDbContext db)
{
    private static RssSourceDto Map(RssSource s) => new(
        s.Id, s.Name, s.Url, s.IsTrusted, s.IsEnabled, s.LastFetchedAt,
        s.SourceType.ToString(),
        s.LinkSelector, s.TitleSelector, s.ContentSelector, s.DateSelector, s.ImageSelector);

    // ? GetAllAsync : возвращает список всех источников
    // вызывается из AdminController.GetSources (Admin)
    public async Task<List<RssSourceDto>> GetAllAsync() =>
        await db.RssSources.Select(s => Map(s)).ToListAsync();

    // ? CreateAsync : добавляет новый источник
    // вызывается из AdminController.CreateSource (Admin)
    public async Task<RssSourceDto> CreateAsync(CreateRssSourceDto dto)
    {
        var source = new RssSource
        {
            Name             = dto.Name,
            Url              = dto.Url,
            IsTrusted        = dto.IsTrusted,
            SourceType       = Enum.Parse<SourceType>(dto.SourceType, true),
            LinkSelector     = dto.LinkSelector,
            TitleSelector    = dto.TitleSelector,
            ContentSelector  = dto.ContentSelector,
            DateSelector     = dto.DateSelector,
            ImageSelector    = dto.ImageSelector
        };
        db.RssSources.Add(source);
        await db.SaveChangesAsync();
        return Map(source);
    }

    // ? UpdateAsync : обновляет данные источника
    // вызывается из AdminController.UpdateSource (Admin)
    public async Task<bool> UpdateAsync(int id, UpdateRssSourceDto dto)
    {
        var source = await db.RssSources.FindAsync(id);
        if (source is null) return false;
        source.Name            = dto.Name;
        source.Url             = dto.Url;
        source.IsTrusted       = dto.IsTrusted;
        source.SourceType      = Enum.Parse<SourceType>(dto.SourceType, true);
        source.LinkSelector    = dto.LinkSelector;
        source.TitleSelector   = dto.TitleSelector;
        source.ContentSelector = dto.ContentSelector;
        source.DateSelector    = dto.DateSelector;
        source.ImageSelector   = dto.ImageSelector;
        await db.SaveChangesAsync();
        return true;
    }

    // ? DeleteAsync : удаляет источник по идентификатору
    // вызывается из AdminController.DeleteSource (Admin)
    public async Task<bool> DeleteAsync(int id)
    {
        var source = await db.RssSources.FindAsync(id);
        if (source is null) return false;
        db.RssSources.Remove(source);
        await db.SaveChangesAsync();
        return true;
    }

    // ? SetEnabledAsync : включает или отключает источник
    // вызывается из AdminController.SetEnabled (Admin)
    public async Task<bool> SetEnabledAsync(int id, bool enabled)
    {
        var source = await db.RssSources.FindAsync(id);
        if (source is null) return false;
        source.IsEnabled = enabled;
        await db.SaveChangesAsync();
        return true;
    }
}
