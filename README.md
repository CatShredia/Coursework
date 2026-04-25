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

## Email в Docker

Для локальной отправки писем в `docker-compose.yml` добавлен `mailpit`:

- SMTP сервер внутри Docker: `mailpit:1025`
- Web-интерфейс писем: `http://localhost:8025`

Переменные в `.env`:

- `SMTP_HOST` (по умолчанию `mailpit`)
- `SMTP_PORT` (по умолчанию `1025`)
- `SMTP_FROM`
- `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_USE_SSL` — для внешнего SMTP (например, Яндекс/Gmail/SendGrid).

## Вариант A: Tuna + внешний SMTP

Рекомендуемая схема для твоего случая (когда ngrok зависит от VPN, а Gmail SMTP нестабилен):

1. Подними Docker-стек приложения:

```bash
docker compose up -d --build
```

2. Создай HTTP-туннель в Tuna на локальный `80` порт (web-контейнер):
   - локальная точка: `http://localhost:80`
   - получи публичный URL в домене Tuna.

3. Пропиши полученный Tuna URL в `.env` (без завершающего `/`):

```env
PUBLIC_URL=https://your-tuna-domain

# SMTP (пример: Brevo)
SMTP_HOST=smtp-relay.brevo.com
SMTP_PORT=587
SMTP_FROM=noreply@your-domain.com
SMTP_USERNAME=your-smtp-login
SMTP_PASSWORD=your-smtp-password-or-api-key
SMTP_USE_SSL=true
SMTP_PREFER_IPV4=false
```

4. Перезапусти API и web после обновления `.env`:

```bash
docker compose up -d --build api web
```

5. Проверь:
   - приложение: `PUBLIC_URL`
   - API health: `PUBLIC_URL/health`
   - регистрация отправляет письмо через внешний SMTP.

Если нужно локальное тестирование почты без внешнего SMTP, оставь `mailpit` (`SMTP_HOST=mailpit`, `SMTP_PORT=1025`) и открой `http://localhost:8025`.
