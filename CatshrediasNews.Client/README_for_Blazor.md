# README for Blazor

`CatshrediasNews.Client` - клиентская часть Runews на Blazor WebAssembly. Клиент отвечает за интерфейс ленты, просмотр статей, регистрацию и вход, профиль, редактор публикаций, админку, модерацию, поиск и работу с API.

## Технологии

- Blazor WebAssembly (.NET 8)
- Razor components
- HttpClient + typed services
- JWT в `localStorage`
- SignalR client для комментариев
- CSS-модули в `wwwroot/css`
- JavaScript interop для загрузки файлов, markdown editor и утилит

## Структура Проекта

```text
CatshrediasNews.Client/
├── Layout/             основные layout-компоненты и сайдбары
├── Pages/              страницы приложения
│   ├── Admin/          администрирование
│   ├── Moderation/     модерация
│   └── Publicist/      работа автора со статьями
├── Services/           клиентские сервисы доступа к API
├── Models/             DTO/модели клиента
├── wwwroot/            статические файлы, CSS, JS, appsettings
├── Program.cs          DI, HttpClient, auth init
└── CatshrediasNews.Client.csproj
```

## Основные Страницы

### Общие

- `Home.razor` - главная лента, фильтры по источникам/тегам, сообщение об успешном подтверждении email.
- `ArticleView.razor` - детальная страница статьи, изображение, контент, комментарии и переход к комментариям.
- `Saved.razor` - сохраненные статьи пользователя.
- `PublicProfile.razor` - публичный профиль автора.
- `About.razor`, `PublishingRules.razor`, `EditorGuide.razor` - информационные страницы.

### Авторизация

- `Login.razor` - вход.
- `Register.razor` - регистрация, выбор цвета и загрузка аватара.
- `ConfirmEmail.razor` - подтверждение email по токену.
- `ForgotPassword.razor` и `ResetPassword.razor` - восстановление пароля.
- `Profile.razor` - профиль, изменение данных, аватар, удаление аккаунта.

### Publicist

- `Publicist/CreateArticle.razor` - создание и редактирование статьи, теги, markdown/editor helpers.
- `Publicist/MyArticles.razor` - список статей автора и их статусы.

### Moderation

- `Moderation/Queue.razor` - очередь статей на проверку.
- `Moderation/Reports.razor` - жалобы пользователей.

### Admin

- `Admin/Users.razor` - управление пользователями.
- `Admin/Tags.razor` - управление тегами.
- `Admin/Rss.razor` - сторонние источники, scraper/RSS-настройки, экспорт SQL.

## Layout

- `MainLayout.razor` - основной layout авторизованной/публичной части.
- `AuthLayout.razor` - layout страниц входа и регистрации.
- `AppHeader.razor` - верхняя панель, поиск, пользовательское меню.
- `LeftSidebar.razor` и `RightSidebar.razor` - навигация и боковые блоки.
- `SidebarItem.razor` - общий пункт меню на базе `NavLink`.

## Services

Клиентские сервисы инкапсулируют HTTP-запросы к API.

- `AuthService` - вход, регистрация, выход, хранение сессии в `localStorage`, профиль.
- `ArticleService` - лента, статьи, теги, лайки, сохранение.
- `CommentService` - комментарии и SignalR-взаимодействие.
- `AdminService` - админские действия: пользователи, теги, источники, экспорт SQL.
- `ModerationService` - очередь модерации и жалобы.
- `GigaChadService` - AI-помощник редактора.
- `ThemeService` - тема интерфейса.
- `ArticleHeadingsService` - работа с заголовками статьи.
- `UnauthorizedLogoutHandler` - глобальная обработка `401`: очистка `localStorage` и перезагрузка страницы.

## Models

- `ArticleDto.cs` - статьи, теги, запросы создания/обновления.
- `UserInfo.cs` - текущий пользователь и профиль.
- `CommentDto.cs` - комментарии и дерево обсуждений.

## Static Files

`wwwroot` содержит:

- `appsettings.json` и `appsettings.Production.json` - URL API для клиента;
- `css/` - стили ленты, статьи, админки, модерации, редактора и layout;
- `js/runews.js` - JS interop утилиты, включая скачивание текстового файла;
- `js/md-editor.js` - вспомогательная логика markdown/editor;
- `index.html` - точка входа Blazor WebAssembly.

## Конфигурация API URL

В hosted Docker-сценарии клиент обслуживается Nginx на том же домене, что и API, поэтому production-конфигурация может использовать:

```json
{
  "Api": {
    "BaseUrl": "/"
  }
}
```

`Program.cs` умеет обработать абсолютный URL, относительный URL и fallback на текущий host.

## Авторизация На Клиенте

После входа JWT и данные пользователя сохраняются в `localStorage`:

- `auth_token`
- `auth_username`
- `auth_email`
- `auth_role`
- `auth_id`
- `auth_avatar_url`
- `auth_avatar_color`

При старте приложения `AuthService.InitAsync()` восстанавливает сессию. Если API возвращает `401`, `UnauthorizedLogoutHandler` очищает auth-данные и принудительно перезагружает страницу.

## Запуск

Локально:

```bash
dotnet run --project CatshrediasNews.Client/CatshrediasNews.Client.csproj
```

## Особенности UI

- Главная лента поддерживает фильтры и поиск.
- Модальное окно поиска использует фильтры по источникам.
- Редактор статьи поддерживает поиск и создание тегов.
- Админская страница источников в production может быть заблокирована для редактирования.
- После подтверждения email пользователь возвращается на главную и видит сообщение об успехе.
