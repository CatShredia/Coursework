using CatshrediasNewsAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Services;

public class RssFetcherService(
    IServiceScopeFactory scopeFactory,
    RssParserService parser,
    IConfiguration config,
    ILogger<RssFetcherService> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(
        config.GetValue<int>("RssFetcher:IntervalMinutes", 15));

    // ? ExecuteAsync : основной цикл фонового сервиса — запускает парсинг всех источников по расписанию
    // запускается автоматически при старте приложения через BackgroundService
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RssFetcherService запущен. Интервал: {Interval} мин.", _interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await FetchAllAsync(stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }
    }

    // ? FetchAllAsync : получает список активных RSS-источников и запускает парсинг каждого
    // вызывается из ExecuteAsync
    private async Task FetchAllAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sources = await db.RssSources.ToListAsync(ct);

        if (sources.Count == 0)
        {
            logger.LogWarning("RSS-источники не найдены в БД.");
            return;
        }

        logger.LogInformation("Запуск парсинга {Count} источников...", sources.Count);

        foreach (var source in sources)
        {
            if (ct.IsCancellationRequested) break;
            await parser.ParseSourceAsync(source);
        }
    }
}
