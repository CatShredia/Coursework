# README for API

`CatshrediasNewsAPI` - серверная часть Runews. API отвечает за пользователей, авторизацию, статьи, комментарии, модерацию, теги, внешние источники, email-подтверждение и realtime-обновления через SignalR.

## Технологии

- ASP.NET Core 8 Web API
- Entity Framework Core
- PostgreSQL
- JWT Bearer authentication
- BCrypt для паролей
- SignalR для комментариев
- HtmlAgilityPack и FeedReader для scraper/RSS
- SMTP для подтверждения email и восстановления пароля

## Структура Проекта

```text
CatshrediasNewsAPI/
├── Controllers/        HTTP endpoints
├── Services/           бизнес-логика приложения
├── Models/             EF Core сущности
├── DTOs/               контракты API
├── Data/               AppDbContext и seed.sql
├── Hubs/               SignalR hubs
├── Migrations/         EF Core migrations
├── wwwroot/            uploads и dev/test статические файлы
├── Program.cs          DI, middleware, auth, CORS, migrations
└── appsettings*.json   конфигурация окружений
```

## Основные Слои

### Controllers

Контроллеры принимают HTTP-запросы, валидируют доступ через атрибуты авторизации и вызывают сервисы.

- `AuthController` - регистрация, вход, подтверждение email, сброс пароля.
- `ArticlesController` - лента, детали статьи, создание/редактирование, лайки, сохраненные статьи.
- `CommentsController` - получение, создание и удаление комментариев.
- `TagsController` - теги и подписки пользователя.
- `UsersController` - профиль, аватар, удаление аккаунта.
- `ModerationController` - очередь модерации, жалобы, подтверждение/отклонение.
- `AdminController` - пользователи, теги, сторонние источники, экспорт SQL.
- `GigaChadAIController` - вспомогательные AI-функции редактора.
- `RssTestController` - dev/test endpoints для проверки источников.

### Services

Сервисы содержат основную бизнес-логику и изолируют контроллеры от деталей БД, SMTP и парсинга.

- `AuthService` - JWT, регистрация, email-confirmation, password reset, инвалидация сессий через версию пароля.
- `EmailService` - отправка писем через SMTP.
- `ArticleService` - статьи, лента, лайки, сохранение, теги.
- `CommentService` - дерево комментариев и права удаления.
- `ModerationService` - проверка статей, жалобы, moderation logs.
- `UserService` - профиль, аватары, блокировка/удаление.
- `TagService` и `TagMappingService` - теги, подписки, сопоставление категорий источников.
- `RssFetcherService`, `RssParserService`, `RssSourceService` - RSS-источники и фоновая загрузка.
- `ScraperService` - загрузка HTML-страниц, извлечение заголовка, текста, даты и изображения по селекторам.
- `GigaChatService` - интеграция с AI-помощником.

### Models

`Models` описывают таблицы и связи PostgreSQL:

- пользователи, роли и профиль: `User`, `Role`;
- публикации: `Article`, `ArticleTag`, `PublicationStatus`, `SavedArticle`, `Like`;
- теги и интересы: `Tag`, `UserTagWeight`;
- обсуждения: `Comment`;
- модерация: `Report`, `ReportType`, `ModerationLog`;
- внешние источники: `RssSource`.

### DTOs

`DTOs` отделяют внутренние EF-сущности от публичного API. Через них передаются данные регистрации, статей, тегов, комментариев, модерации и RSS-источников.

### Data

- `AppDbContext.cs` - схема БД, связи, индексы и настройки EF Core.
- `seed.sql` - стартовые роли, статусы, теги, типы жалоб, пользователи и внешние источники.

### Hubs

`CommentsHub` используется для realtime-комментариев. Клиент подключается к `/hubs/comments`, подписывается на конкретную статью и получает события о новых/удаленных комментариях.

## Конфигурация

Ключевые секции:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=news_db;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "32+ chars secret",
    "Issuer": "https://public-url",
    "Audience": "https://public-url"
  },
  "App": {
    "BaseUrl": "https://public-url"
  },
  "Api": {
    "BaseUrl": "https://public-url"
  },
  "Cors": {
    "AllowedOrigins": ["https://public-url"]
  },
  "Smtp": {
    "Host": "smtp-relay.example.com",
    "Port": "587",
    "From": "noreply@example.com",
    "Username": "login",
    "Password": "password-or-api-key",
    "UseSsl": "true",
    "PreferIpv4": "false"
  }
}
```

В Docker эти значения прокидываются через переменные окружения из `.env`.

## Аутентификация И Сессии

API использует JWT Bearer. Токен содержит:

- id пользователя;
- email;
- роль;
- claim версии пароля (`pwdv`).

При каждом защищенном запросе API проверяет, что пользователь существует, не заблокирован и версия пароля в токене совпадает с текущим `PasswordHash`. Если пароль изменен или аккаунт удален/заблокирован, старые токены становятся недействительными.

## Email

Email используется для:

- подтверждения аккаунта после регистрации;
- повторной отправки подтверждения для неподтвержденных аккаунтов;
- восстановления пароля.

Ссылки строятся от `App:BaseUrl`, поэтому для туннелей Tuna или production-домена важно корректно заполнить `PUBLIC_URL`.

## RSS И Scraper

Внешние источники хранятся в таблице `RssSources`.

- RSS-источники обрабатываются через `RssFetcherService` и `RssParserService`.
- Scraper-источники используют CSS/XPath-селекторы из БД.
- Доверенные источники могут публиковаться сразу, остальные проходят модерацию.
- Админка умеет экспортировать текущие источники в SQL.

## Модерация

Модераторы работают с очередью статей и жалобами:

- одобряют или отклоняют статьи;
- подтверждают или отклоняют жалобы;
- не могут модерировать собственные статьи;
- действия фиксируются в `ModerationLogs`.

## Запуск

Локально через .NET:

```bash
dotnet restore
dotnet ef database update
dotnet run --project CatshrediasNewsAPI/CatshrediasNewsAPI.csproj
```

Через Docker из корня проекта:

```bash
docker compose up -d --build
```

Проверка:

- `/health` - health endpoint;
- `/swagger` - Swagger UI в Development;
- `/hubs/comments` - SignalR hub.

## Seed.sql

В Docker:

```bash
docker compose exec -T db psql -U postgres -d news_db < CatshrediasNewsAPI/Data/seed.sql
```

Из DataGrip:

- Host: `127.0.0.1`
- Port: `55432`
- Database: значение `POSTGRES_DB`
- User/Password: значения из `.env`
