# CatshrediasNews API

REST API для новостного агрегатора с системой модерации, персональными рекомендациями и RSS-интеграцией.

---

## Стек технологий

| Компонент | Технология |
|---|---|
| Фреймворк | ASP.NET Core 8 Web API |
| ORM | Entity Framework Core 8 |
| База данных | PostgreSQL |
| Аутентификация | JWT Bearer |
| Хеширование паролей | BCrypt.Net |
| Реалтайм | SignalR |
| RSS-парсинг | CodeHollow.FeedReader |
| Очистка HTML | HtmlAgilityPack |
| Документация | Swagger / OpenAPI |

---

## Быстрый старт

### 1. Требования

- .NET 8 SDK
- PostgreSQL 14+

### 2. Настройка `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<host>;Port=5432;Database=<db>;Username=<user>;Password=<password>"
  },
  "Jwt": {
    "Key": "<секретный_ключ_минимум_32_символа>",
    "Issuer": "CatshrediasNewsAPI",
    "Audience": "CatshrediasNewsClient"
  },
  "RssFetcher": {
    "IntervalMinutes": 15,
    "TagMappingRules": { ... }
  }
}
```

### 3. Применение миграций и запуск

```bash
dotnet ef database update
dotnet run
```

### 4. Тестовые данные

```bash
psql -U <user> -d <db> -f Data/seed.sql
```

### 5. Swagger UI

```
https://localhost:7240/swagger
```

Для авторизованных запросов нажмите **Authorize** и вставьте JWT-токен **без** префикса `Bearer`.

---

## Структура проекта

```
CatshrediasNewsAPI/
├── Controllers/       — HTTP-эндпоинты
├── Services/          — бизнес-логика
├── Models/            — сущности БД
├── DTOs/              — объекты передачи данных
├── Data/              — DbContext, seed.sql
├── Hubs/              — SignalR хабы
├── Migrations/        — миграции EF Core
└── wwwroot/           — статические страницы для тестирования
```

---

## Аутентификация

Все защищённые эндпоинты требуют заголовок:

```
Authorization: Bearer <jwt_token>
```

Токен получается при регистрации или входе. Срок действия — 7 дней.

### Роли

| Роль | Описание |
|---|---|
| `User` | Обычный пользователь |
| `Moderator` | Модерация контента и жалоб |
| `Admin` | Полный доступ, управление источниками и тегами |

---

## API Endpoints

### Auth — `/api/auth`

| Метод | Путь | Описание | Доступ |
|---|---|---|---|
| POST | `/register` | Регистрация нового пользователя | Public |
| POST | `/login` | Вход, возвращает JWT-токен | Public |

**Пример запроса регистрации:**
```json
POST /api/auth/register
{
  "username": "alex",
  "email": "alex@example.com",
  "password": "password123"
}
```

**Пример ответа:**
```json
{
  "token": "eyJhbGci...",
  "user": {
    "id": 1,
    "username": "alex",
    "email": "alex@example.com",
    "role": "User",
    "isBlocked": false
  }
}
```

---

### Users — `/api/users`

| Метод | Путь | Описание | Доступ |
|---|---|---|---|
| GET | `/{id}` | Публичный профиль пользователя | Public |
| GET | `/me` | Профиль текущего пользователя | Auth |
| PUT | `/me` | Редактирование профиля | Auth |
| DELETE | `/me` | Удаление аккаунта | Auth |

**Пример тела PUT `/me`** (все поля опциональны):
```json
{
  "username": "new_name",
  "email": "new@example.com",
  "password": "newpassword"
}
```

---

### Articles — `/api/articles`

| Метод | Путь | Описание | Доступ |
|---|---|---|---|
| GET | `/` | Хронологическая лента | Public |
| GET | `/feed` | Персональная лента (по весам тегов) | Auth |
| GET | `/{id}` | Детальная страница статьи | Public |
| POST | `/` | Создать статью (статус PendingReview) | Auth |
| POST | `/{id}/like` | Поставить / снять лайк | Auth |

Параметры пагинации: `?page=1&pageSize=20`

**Пример тела POST `/`:**
```json
{
  "title": "Заголовок статьи",
  "content": "Текст статьи...",
  "sourceUrl": null,
  "publishedAt": "2026-04-17T12:00:00Z",
  "tagIds": [1, 6]
}
```

---

### Tags — `/api/tags`

| Метод | Путь | Описание | Доступ |
|---|---|---|---|
| GET | `/` | Список всех тегов | Public |
| POST | `/` | Создать тег | Admin |
| DELETE | `/{id}` | Удалить тег | Admin |
| PUT | `/subscriptions` | Обновить подписки на теги | Auth |

**Пример тела PUT `/subscriptions`:**
```json
{ "tagIds": [1, 3, 6] }
```

---

### Comments — `/api/articles/{articleId}/comments`

| Метод | Путь | Описание | Доступ |
|---|---|---|---|
| GET | `/` | Дерево комментариев статьи | Public |
| POST | `/` | Добавить комментарий | Auth |
| DELETE | `/{commentId}` | Удалить комментарий | Auth (автор или модератор) |

**Пример тела POST:**
```json
{
  "content": "Текст комментария",
  "parentCommentId": null
}
```

---

### Moderation — `/api/moderation`

| Метод | Путь | Описание | Доступ |
|---|---|---|---|
| GET | `/queue` | Очередь статей на проверку | Moderator |
| POST | `/{id}/approve` | Одобрить статью | Moderator |
| POST | `/{id}/reject` | Отклонить статью с причиной | Moderator |
| GET | `/reports` | Список жалоб | Moderator |
| POST | `/articles/{articleId}/report` | Пожаловаться на статью | Auth |

**Пример тела POST `/{id}/reject`:**
```json
{ "reason": "Нарушение правил сообщества" }
```

**Пример тела POST `/articles/{id}/report`:**
```json
{
  "reportTypeId": 3,
  "description": "Статья содержит недостоверную информацию"
}
```

Типы жалоб: `1 — Spam`, `2 — Hate`, `3 — Fake`, `4 — Violence`, `5 — Copyright`

---

### Admin — `/api/admin`

#### RSS-источники

| Метод | Путь | Описание |
|---|---|---|
| GET | `/sources` | Список всех источников |
| POST | `/sources` | Добавить источник |
| PUT | `/sources/{id}` | Обновить источник |
| DELETE | `/sources/{id}` | Удалить источник |
| POST | `/sources/{id}/enable` | Включить источник |
| POST | `/sources/{id}/disable` | Отключить источник |

**Пример тела POST `/sources`:**
```json
{
  "name": "Habr",
  "url": "https://habr.com/ru/rss/articles/",
  "isTrusted": true
}
```

#### Управление RSS-фетчером

| Метод | Путь | Описание |
|---|---|---|
| GET | `/rss/status` | Текущий интервал парсинга |
| PUT | `/rss/interval` | Изменить интервал (в минутах) |
| POST | `/rss/trigger` | Принудительно запустить парсинг |

**Пример тела PUT `/rss/interval`:**
```json
{ "intervalMinutes": 30 }
```

#### Теги

| Метод | Путь | Описание |
|---|---|---|
| GET | `/tags` | Список всех тегов |
| POST | `/tags` | Создать тег |
| DELETE | `/tags/{id}` | Удалить тег |

---

## SignalR — Хаб комментариев

**URL подключения:** `/hubs/comments`

JWT-токен передаётся через `accessTokenFactory`:

```js
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/comments", { accessTokenFactory: () => token })
    .build();
```

### Методы (клиент → сервер)

| Метод | Параметры | Описание |
|---|---|---|
| `JoinArticle` | `articleId: int` | Подписаться на комментарии статьи |
| `LeaveArticle` | `articleId: int` | Отписаться от комментариев статьи |

### События (сервер → клиент)

| Событие | Данные | Когда |
|---|---|---|
| `ReceiveComment` | `CommentDto` | После добавления нового комментария |
| `CommentDeleted` | `commentId: int` | После удаления комментария |

---

## RSS-интеграция

### Как работает

1. Источники хранятся в таблице `RssSources`. Поле `IsEnabled` включает/отключает источник без удаления.
2. `RssFetcherService` — фоновый сервис, запускается при старте и обходит все включённые источники по расписанию.
3. `RssParserService` парсит фид, очищает HTML из `<description>`, проверяет дубли по `guid`.
4. Статус новой статьи: `Published` — для доверенных источников (`IsTrusted = true`), `PendingReview` — для остальных.
5. `TagMappingService` сопоставляет RSS-категории с тегами БД по правилам из `appsettings.json`.

### Настройка правил маппинга тегов

```json
"TagMappingRules": {
  "IT":         ["rust", "c#", "python", "docker"],
  "Технологии": ["ai", "нейросети", "llm"],
  "Наука":      ["физика", "биология", "квантовые"]
}
```

Ключ — название тега в БД, значение — список ключевых слов для поиска в RSS-категориях статьи.

---

## Система рекомендаций

Персональная лента (`GET /api/articles/feed`) сортирует статьи по сумме весов тегов пользователя:

- Лайк статьи → вес тегов этой статьи увеличивается на `+0.5`
- Снятие лайка → вес уменьшается на `-0.5`
- Подписка на тег → `IsSubscribed = true`, начальный вес `1.0`

Для новых пользователей без истории возвращается хронологическая лента.

---

## Схема базы данных

| Таблица | Описание |
|---|---|
| `Roles` | Справочник ролей (Admin, Moderator, User) |
| `Users` | Пользователи |
| `PublicationStatuses` | Статусы статей (Draft, PendingReview, Published, Rejected) |
| `RssSources` | RSS-источники |
| `Tags` | Глобальный список тегов |
| `Articles` | Статьи и новости |
| `ArticleTags` | Many-to-Many: статьи ↔ теги |
| `UserTagWeights` | Профиль интересов пользователя (теги + веса) |
| `Likes` | История лайков |
| `SavedArticles` | Сохранённые статьи |
| `Comments` | Комментарии (древовидные) |
| `ReportTypes` | Типы жалоб (Spam, Hate, Fake, Violence, Copyright) |
| `Reports` | Жалобы пользователей |
| `ModerationLogs` | Журнал действий модераторов |

---

## Тестовые страницы

Доступны только в режиме разработки.

| URL | Описание |
|---|---|
| `/swagger` | Swagger UI — документация и тестирование всех эндпоинтов |
| `/hub-test.html` | Тестирование SignalR хаба комментариев |
| `/rss-test.html` | Тестирование RSS-интеграции: предпросмотр фидов, ручной запуск парсинга, просмотр результатов |
