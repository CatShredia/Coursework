#!/usr/bin/env python3
"""Apply localization replacements to Blazor razor files."""
from pathlib import Path

ROOT = Path("c:/directory-git/Coursework/CatshrediasNews.Client")

REPLACEMENTS = [
    ('<PageTitle>Лента — Runews</PageTitle>', '<PageTitle>@L["Home_Title"]</PageTitle>'),
    ('<strong>Аккаунт удалён.</strong>', '<strong>@L["Home_AccountDeleted"]</strong>'),
    ('Спасибо, что были с нами.', '@L["Home_AccountDeletedThanks"]'),
    ('<strong>Email успешно подтверждён.</strong>', '<strong>@L["Home_EmailConfirmed"]</strong>'),
    ('Теперь вы можете войти в аккаунт.', '@L["Home_EmailConfirmedLogin"]'),
    ('<span>Для вас</span>', '<span>@L["Home_ForYou"]</span>'),
    ('<span>Лента новостей</span>', '<span>@L["Home_Feed"]</span>'),
    ('<i class="bi bi-clock"></i> Новые', '<i class="bi bi-clock"></i> @L["Home_SortNewest"]'),
    ('<i class="bi bi-clock-history"></i> Старые', '<i class="bi bi-clock-history"></i> @L["Home_SortOldest"]'),
    ('<i class="bi bi-fire"></i> Популярные', '<i class="bi bi-fire"></i> @L["Home_SortPopular"]'),
    ('<span>Лента персонализирована на основе ваших интересов</span>', '<span>@L["Home_RecEnabled"]</span>'),
    ('<span>Персональные рекомендации отключены — показана общая лента</span>', '<span>@L["Home_RecDisabled"]</span>'),
    ('@(_personalizedFeedEnabled ? "Отключить функцию" : "Включить функцию")', '@(_personalizedFeedEnabled ? L["Home_RecDisable"] : L["Home_RecEnable"])'),
    ('Все источники', '@L["Home_AllSources"]'),
    ('Авторские', '@L["Home_AuthorsOnly"]'),
    ('Статей не найдено. Попробуйте изменить фильтры.', '@L["Home_Empty"]'),
    ('Рекомендовано', '@L["Home_Recommended"]'),
    ('title="Сохранить"', 'title="@L["Home_Save"]"'),
    ('title="Убрать из избранного"', 'title="@L["Home_Unsave"]"'),
    ('<PageTitle>Вход — Runews</PageTitle>', '<PageTitle>@L["Auth_PageLogin"]</PageTitle>'),
    ('<PageTitle>Регистрация — Runews</PageTitle>', '<PageTitle>@L["Auth_PageRegister"]</PageTitle>'),
    ('<i class="bi bi-arrow-left"></i> Назад', '<i class="bi bi-arrow-left"></i> @L["Common_Back"]'),
    ('<h1 class="auth-title">Вход в аккаунт</h1>', '<h1 class="auth-title">@L["Auth_LoginTitle"]</h1>'),
    ('<h1 class="auth-title">Создать аккаунт</h1>', '<h1 class="auth-title">@L["Auth_RegisterTitle"]</h1>'),
    ('<span>Подтвердите email перед входом. Проверьте почту.</span>', '<span>@L["Auth_EmailNotConfirmed"]</span>'),
    ('<i class="bi bi-check-circle"></i> Email подтверждён! Теперь можно войти.', '<i class="bi bi-check-circle"></i> @L["Auth_EmailConfirmed"]'),
    ('<label>Email</label>', '<label>@L["Auth_Email"]</label>'),
    ('<label>Пароль</label>', '<label>@L["Auth_Password"]</label>'),
    ('placeholder="Введите пароль"', 'placeholder="@L["Auth_PasswordEnter"]"'),
    ('<span class="field-error">Введите корректный email (например: you@example.com)</span>', '<span class="field-error">@L["Auth_ErrorEmail"]</span>'),
    ('<span class="field-error">Пароль не может быть пустым</span>', '<span class="field-error">@L["Auth_ErrorPasswordEmpty"]</span>'),
    ('<span>Войти</span>', '<span>@L["Auth_LoginBtn"]</span>'),
    ('<PageTitle>Избранное — Runews</PageTitle>', '<PageTitle>@L["Saved_Title"]</PageTitle>'),
    ('<h1 class="page-title" style="margin:0">Избранное</h1>', '<h1 class="page-title" style="margin:0">@L["Saved_Heading"]</h1>'),
    ('Вы ещё ничего не сохранили.', '@L["Saved_Empty"]'),
    ('← В ленту', '@L["Saved_BackToFeed"]'),
    ('<PageTitle>Очередь модерации — Runews</PageTitle>', '<PageTitle>@L["Mod_QueueTitle"]</PageTitle>'),
    ('<h1 class="page-title" style="margin:0">Очередь модерации</h1>', '<h1 class="page-title" style="margin:0">@L["Mod_QueueHeading"]</h1>'),
    ('Очередь пуста — все статьи проверены.', '@L["Mod_QueueEmpty"]'),
    ('<i class="bi bi-search"></i> Проверить', '<i class="bi bi-search"></i> @L["Mod_Review"]'),
    ('<i class="bi bi-check-lg"></i> Одобрить', '<i class="bi bi-check-lg"></i> @L["Mod_Approve"]'),
    ('<i class="bi bi-x-lg"></i> Отклонить', '<i class="bi bi-x-lg"></i> @L["Mod_Reject"]'),
    ('Загрузка...', '@L["Common_Loading"]'),
    ('<PageTitle>Проверка статьи — Runews</PageTitle>', '<PageTitle>@L["Mod_ReviewTitle"]</PageTitle>'),
    ('<i class="bi bi-arrow-left"></i> Очередь', '<i class="bi bi-arrow-left"></i> @L["Mod_BackQueue"]'),
    ('Статья не найдена в очереди.', '@L["Mod_NotInQueue"]'),
    ('Вернуться в очередь', '@L["Mod_ReturnQueue"]'),
    ('Исходный текст (выделяйте здесь)', '@L["Mod_SourceText"]'),
    ('Добавить замечание по выделению', '@L["Mod_AddNote"]'),
    ('<h3 class="mod-review-col-title">Предпросмотр</h3>', '<h3 class="mod-review-col-title">@L["Mod_Preview"]</h3>'),
    ('Общий комментарий (необязательно)', '@L["Mod_Summary"]'),
    ('placeholder="Краткий итог для автора"', 'placeholder="@L["Mod_SummaryPlaceholder"]"'),
    ('Замечание к фрагменту', '@L["Mod_NoteTitle"]'),
    ('placeholder="Причина (обязательно)"', 'placeholder="@L["Mod_NoteReason"]"'),
    ('Укажите причину (минимум 3 символа).', '@L["Mod_NoteReasonMin"]'),
    ('Отмена', '@L["Common_Cancel"]'),
    ('Добавить', '@L["Common_Add"]'),
    ('<PageTitle>Мои статьи — Runews</PageTitle>', '<PageTitle>@L["Editor_MyArticles"]</PageTitle>'),
    ('<h1 class="page-title" style="margin:0">Мои статьи</h1>', '<h1 class="page-title" style="margin:0">@L["Editor_MyArticlesHeading"]</h1>'),
    ('<i class="bi bi-pencil-square"></i> Написать статью', '<i class="bi bi-pencil-square"></i> @L["Nav_WriteArticle"]'),
    ('Статей нет.', '@L["Editor_NoArticles"]'),
    ('Написать первую?', '@L["Editor_WriteFirst"]'),
    ('Причина отклонения', '@L["Editor_RejectionTitle"]'),
    ('Причина не указана', '@L["Editor_RejectionUnknown"]'),
    ('Замечания модератора', '@L["Editor_ModeratorNotes"]'),
    ('Удалить статью?', '@L["Editor_DeleteTitle"]'),
    ('Включен режим разработчика на сервере', '@L["Nav_DevBanner"]'),
    ('title="Скрыть меню"', 'title="@L["Nav_HideMenu"]'),
    ('title="Показать меню"', 'title="@L["Nav_ShowMenu"]'),
    ('title="Скрыть содержание"', 'title="@L["Nav_HideToc"]'),
    ('title="Показать содержание"', 'title="@L["Nav_ShowToc"]'),
    ('@(EditId.HasValue ? "Редактировать статью" : "Написать статью")', '@(EditId.HasValue ? L["Editor_Edit"] : L["Editor_Write"])'),
    ('<PageTitle>@(EditId.HasValue ? "Редактировать статью" : "Написать статью") — Runews</PageTitle>',
     '<PageTitle>@(EditId.HasValue ? L["Editor_EditTitle"] : L["Editor_CreateTitle"])</PageTitle>'),
    ('@(_preview ? "Редактор" : "Предпросмотр")', '@(_preview ? L["Editor_EditorMode"] : L["Editor_Preview"])'),
    ('Черновик', '@L["Editor_Draft"]'),
    ('Сохранить', '@L["Common_Save"]'),
    ('Замечания модератора', '@L["Editor_ModeratorNotes"]'),
    ('Подсветка фрагментов отображается в режиме «Предпросмотр».', '@L["Editor_HighlightHint"]'),
    ('placeholder="Заголовок статьи..."', 'placeholder="@L["Editor_TitlePlaceholder"]"'),
    ('placeholder="Начните писать статью в формате Markdown..."', 'placeholder="@L["Editor_ContentPlaceholder"]"'),
    ('Нет содержимого для предпросмотра.', '@L["Editor_PreviewEmpty"]'),
    ('Введите заголовок.', '@L["Editor_ErrorTitle"]'),
    ('Статья не может быть пустой.', '@L["Editor_ErrorContent"]'),
    ('Черновик сохранён.', '@L["Editor_DraftSaved"]'),
    ('Статья обновлена и отправлена на модерацию.', '@L["Editor_Saved"]'),
    ('Статья создана и отправлена на модерацию.', '@L["Editor_Created"]'),
    ('new System.Globalization.CultureInfo("ru-RU")', 'System.Globalization.CultureInfo.CurrentCulture'),
    ('>Все</button>', '>@L["Common_All"]</button>'),
    ('<th>Заголовок</th>', '<th>@L["Editor_ColTitle"]</th>'),
    ('<th>Теги</th>', '<th>@L["Editor_ColTags"]</th>'),
    ('<th>Статус</th>', '<th>@L["Editor_ColStatus"]</th>'),
    ('<th>Дата</th>', '<th>@L["Editor_ColDate"]</th>'),
    ('<th>Лайки</th>', '<th>@L["Editor_ColLikes"]</th>'),
    ('<th>Действия</th>', '<th>@L["Editor_ColActions"]</th>'),
    ('title="Открыть"', 'title="@L["Editor_Open"]"'),
    ('title="Редактировать"', 'title="@L["Editor_EditBtn"]"'),
    ('title="Удалить"', 'title="@L["Common_Delete"]"'),
    ('>Закрыть</button>', '>@L["Common_Close"]</button>'),
    ('«@_deleteTarget.Title» будет удалена безвозвратно.', '@L["Editor_DeleteBody", _deleteTarget.Title]'),
    ('<PageTitle>Пользователи — Runews</PageTitle>', '<PageTitle>@L["Admin_UsersPageTitle"]</PageTitle>'),
    ('<h1 class="page-title" style="margin:0">Управление пользователями</h1>', '<h1 class="page-title" style="margin:0">@L["Admin_UsersTitle"]</h1>'),
    ('<span class="admin-count">Всего: @_users.Count</span>', '<span class="admin-count">@L["Common_Total", _users.Count]</span>'),
]

# Fix Mod_OnReview - need special handling for queue count
SPECIAL_FILES = {}

def process_file(path: Path) -> bool:
    text = path.read_text(encoding="utf-8")
    orig = text
    for old, new in REPLACEMENTS:
        text = text.replace(old, new)
    # MyArticles status helpers
    if path.name == "MyArticles.razor" and "StatusLabel" in text:
        text = text.replace(
            'private static string StatusLabel(string s) => s switch\n    {\n        "Draft"         => "Черновик",\n        "PendingReview" => "На проверке",\n        "Published"     => "Опубликована",\n        "Rejected"      => "Отклонена",\n        _               => s\n    };',
            'private string StatusLabel(string s) => L.ArticleStatus(s);'
        )
    if path.name == "Queue.razor":
        text = text.replace(
            '<span class="admin-count">На проверке: @_queue.Count</span>',
            '<span class="admin-count">@L["Mod_OnReview", _queue.Count]</span>'
        )
    if path.name == "Review.razor":
        text = text.replace('Замечания (@_notes.Count)', '@L["Mod_Notes", _notes.Count]')
        text = text.replace('Отклонить (@_notes.Count)', '@L["Mod_RejectCount", _notes.Count]')
        text = text.replace('«@_rejectTarget.Title»', '')  # skip
    if text != orig:
        path.write_text(text, encoding="utf-8")
        return True
    return False

count = 0
for p in ROOT.rglob("*.razor"):
    if process_file(p):
        count += 1
        print("updated", p.relative_to(ROOT))

print("done", count, "files")
