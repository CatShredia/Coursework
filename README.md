# Coursework

## Deployment Ready

Проект подготовлен для выгрузки на сервер:

- API больше не привязан к `localhost` в runtime (жесткие URL остаются только для `Development`);
- CORS вынесен в конфиг `Cors:AllowedOrigins`;
- клиентский Blazor берет API URL из `CatshrediasNews.Client/wwwroot/appsettings*.json` (`Api:BaseUrl`);
- добавлены production-шаблоны:
  - `CatshrediasNewsAPI/appsettings.Production.json`
  - `CatshrediasNews.Client/wwwroot/appsettings.Production.json`
- добавлен health endpoint API: `/health`.

## Что заполнить перед деплоем

1. В `CatshrediasNewsAPI/appsettings.Production.json`:
   - `ConnectionStrings:DefaultConnection`
   - `Jwt:Key` (секрет 32+ символов)
   - `Jwt:Issuer` (URL API, например `https://api.example.com`)
   - `Jwt:Audience` (URL фронтенда, например `https://example.com`)
   - `Smtp:*`
   - `Cors:AllowedOrigins` (домен фронтенда)

2. В `CatshrediasNews.Client/wwwroot/appsettings.Production.json`:
   - `Api:BaseUrl` (публичный URL API с `/` в конце)

## Публикация

```bash
dotnet publish CatshrediasNewsAPI/CatshrediasNewsAPI.csproj -c Release -o publish/api
dotnet publish CatshrediasNews.Client/CatshrediasNews.Client.csproj -c Release -o publish/client
```

## Проверка после запуска

- API health: `https://<api-domain>/health`
- Swagger (если `Development`): `https://<api-domain>/swagger`

## Единая настройка ngrok

Чтобы указывать ngrok URL в одном месте для API и Blazor:

1. Открой `ngrok.settings.json` в корне проекта и задай:
   - `PublicUrl`: твой URL ngrok (без завершающего `/`).
2. Примени настройки командой:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\apply-ngrok.ps1
```

Скрипт автоматически обновляет:

- `CatshrediasNewsAPI/appsettings.json` (`Cors:AllowedOrigins`, `App:BaseUrl`, `Api:BaseUrl`)
- `CatshrediasNews.Client/wwwroot/appsettings.json` (`Api:BaseUrl`)

## Docker Postgres Port

В `docker-compose.yml` PostgreSQL проброшен на хост-порт `55432`:

- Host: `127.0.0.1`
- Port: `55432`
- Database/User/Password: значения из `.env`
