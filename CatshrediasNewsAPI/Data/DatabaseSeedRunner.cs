using System.Data;
using Microsoft.EntityFrameworkCore;

namespace CatshrediasNewsAPI.Data;

public static class DatabaseSeedRunner
{
    public static async Task ApplySeedAsync(
        AppDbContext db,
        IWebHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var seedPath = Path.Combine(environment.ContentRootPath, "Data", "seed.sql");
        if (!File.Exists(seedPath))
        {
            logger.LogWarning("Файл seed.sql не найден: {SeedPath}", seedPath);
            return;
        }

        var sql = await File.ReadAllTextAsync(seedPath, cancellationToken);
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;

        if (openedHere)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 180;
            await command.ExecuteNonQueryAsync(cancellationToken);
            logger.LogInformation("Тестовые данные seed.sql применены успешно.");
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }
}
