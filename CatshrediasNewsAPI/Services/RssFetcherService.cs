using CatshrediasNewsAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Services;

public class RssFetcherService(
    IServiceScopeFactory scopeFactory,
    RssParserService parser,
    ScraperService scraper,
    IConfiguration config,
    ILogger<RssFetcherService> logger) : BackgroundService
{
    private TimeSpan _interval = TimeSpan.FromMinutes(
        config.GetValue<int>("RssFetcher:IntervalMinutes", 15));

    // Сигнал для принудительного немедленного запуска
    private readonly SemaphoreSlim _forceTrigger = new(0, 1);

    public TimeSpan CurrentInterval => _interval;

    // ? SetInterval : изменяет интервал автоматического парсинга без перезапуска сервиса
    // вызывается из AdminController.SetRssInterval (Admin)
    public void SetInterval(int minutes)
    {
        _interval = TimeSpan.FromMinutes(minutes);
        logger.LogInformation("Интервал RSS-парсинга изменён на {Minutes} мин.", minutes);
    }

    // ? TriggerNow : немедленно запускает парсинг, не дожидаясь следующего цикла
    // вызывается из AdminController.TriggerRss (Admin)
    public void TriggerNow()
    {
        // Если семафор уже сигнализирован — не добавляем повторно
        if (_forceTrigger.CurrentCount == 0)
            _forceTrigger.Release();
    }

    // ? ExecuteAsync : основной цикл фонового сервиса — ждёт таймер или принудительный сигнал
    // запускается автоматически при старте приложения через BackgroundService
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RssFetcherService запущен. Интервал: {Interval} мин.", _interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchAllAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка фонового парсинга RSS. Повтор будет выполнен в следующем цикле.");
            }

            // Ждём либо истечения интервала, либо принудительного сигнала
            var delayTask  = Task.Delay(_interval, stoppingToken);
            var triggerTask = _forceTrigger.WaitAsync(stoppingToken);

            await Task.WhenAny(delayTask, triggerTask);
        }
    }

    // ? FetchAllAsync : получает список включённых RSS-источников и запускает парсинг каждого
    // вызывается из ExecuteAsync
    private async Task FetchAllAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sources = await db.RssSources
            .Where(s => s.IsEnabled)
            .ToListAsync(ct);

        if (sources.Count == 0)
        {
            logger.LogWarning("Нет включённых RSS-источников.");
            return;
        }

        logger.LogInformation("Запуск парсинга {Count} источников...", sources.Count);

        foreach (var source in sources)
        {
            if (ct.IsCancellationRequested) break;
            if (source.SourceType == Models.SourceType.Scraper)
                await scraper.ParseSourceAsync(source);
            else
                await parser.ParseSourceAsync(source);
        }
    }
}
