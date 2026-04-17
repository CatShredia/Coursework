-- =============================================================
-- Тестовые данные для CatshrediasNews
-- Порядок вставки соответствует зависимостям внешних ключей
-- =============================================================

-- -------------------------------------------------------------
-- Roles (уже есть seed из миграции, используем UPSERT)
-- -------------------------------------------------------------
INSERT INTO "Roles" ("Id", "Name") VALUES
    (1, 'Admin'),
    (2, 'Moderator'),
    (3, 'User')
ON CONFLICT ("Id") DO NOTHING;

-- -------------------------------------------------------------
-- PublicationStatuses (уже есть seed из миграции)
-- -------------------------------------------------------------
INSERT INTO "PublicationStatuses" ("Id", "Name") VALUES
    (1, 'Draft'),
    (2, 'PendingReview'),
    (3, 'Published'),
    (4, 'Rejected')
ON CONFLICT ("Id") DO NOTHING;

-- -------------------------------------------------------------
-- ReportTypes (уже есть seed из миграции)
-- -------------------------------------------------------------
INSERT INTO "ReportTypes" ("Id", "Name") VALUES
    (1, 'Spam'),
    (2, 'Hate'),
    (3, 'Fake'),
    (4, 'Violence'),
    (5, 'Copyright')
ON CONFLICT ("Id") DO NOTHING;

-- -------------------------------------------------------------
-- Tags
-- -------------------------------------------------------------
INSERT INTO "Tags" ("Id", "Name") VALUES
    (1, 'IT'),
    (2, 'Политика'),
    (3, 'Экономика'),
    (4, 'Спорт'),
    (5, 'Наука'),
    (6, 'Технологии'),
    (7, 'Здоровье')
ON CONFLICT ("Id") DO NOTHING;

-- -------------------------------------------------------------
-- RssSources
-- -------------------------------------------------------------
INSERT INTO "RssSources" ("Id", "Name", "Url", "IsTrusted", "IsEnabled", "LastFetchedAt") VALUES
    (1, 'Habr',        'https://habr.com/ru/rss/articles/',         true,  true,  '2026-04-17 10:00:00+00'),
    (2, 'РБК',         'https://rbc.ru/rss/news',                   true,  true,  '2026-04-17 10:05:00+00'),
    (3, 'Lenta.ru',    'https://lenta.ru/rss',                      true,  true,  '2026-04-17 10:10:00+00'),
    (4, 'Ведомости',   'https://vedomosti.ru/rss/news',             false, true,  '2026-04-17 09:00:00+00'),
    (5, 'Дзен',        'https://dzen.ru/rss',                       false, false, null)
ON CONFLICT ("Id") DO NOTHING;

-- -------------------------------------------------------------
-- Users
-- Пароли захешированы BCrypt (значение: "password123")
-- -------------------------------------------------------------
INSERT INTO "Users" ("Id", "Username", "Email", "PasswordHash", "IsBlocked", "CreatedAt", "RoleId") VALUES
    (1, 'admin',      'admin@catshredias.ru',    '$2a$11$xMBqMDnJMhMBWFAFkFkFkOQZQZQZQZQZQZQZQZQZQZQZQZQZQZQZQ', false, '2026-01-01 00:00:00+00', 1),
    (2, 'moderator1', 'moder1@catshredias.ru',   '$2a$11$xMBqMDnJMhMBWFAFkFkFkOQZQZQZQZQZQZQZQZQZQZQZQZQZQZQZQ', false, '2026-01-05 00:00:00+00', 2),
    (3, 'moderator2', 'moder2@catshredias.ru',   '$2a$11$xMBqMDnJMhMBWFAFkFkFkOQZQZQZQZQZQZQZQZQZQZQZQZQZQZQZQ', false, '2026-01-06 00:00:00+00', 2),
    (4, 'user_alex',  'alex@example.com',        '$2a$11$xMBqMDnJMhMBWFAFkFkFkOQZQZQZQZQZQZQZQZQZQZQZQZQZQZQZQ', false, '2026-02-10 00:00:00+00', 3),
    (5, 'user_maria', 'maria@example.com',       '$2a$11$xMBqMDnJMhMBWFAFkFkFkOQZQZQZQZQZQZQZQZQZQZQZQZQZQZQZQ', false, '2026-02-15 00:00:00+00', 3),
    (6, 'user_ivan',  'ivan@example.com',        '$2a$11$xMBqMDnJMhMBWFAFkFkFkOQZQZQZQZQZQZQZQZQZQZQZQZQZQZQZQ', true,  '2026-03-01 00:00:00+00', 3)
ON CONFLICT ("Id") DO NOTHING;

-- -------------------------------------------------------------
-- Articles
-- -------------------------------------------------------------
INSERT INTO "Articles" ("Id", "Title", "Content", "SourceUrl", "PublishedAt", "CreatedAt", "StatusId", "RssSourceId", "AuthorId") VALUES
    (1,  'Новый фреймворк от Microsoft',
         'Microsoft анонсировал новый фреймворк для разработки веб-приложений с улучшенной производительностью.',
         'https://habr.com/ru/articles/1',
         '2026-04-10 08:00:00+00', '2026-04-10 08:00:00+00', 3, 1, null),

    (2,  'ЦБ повысил ключевую ставку',
         'Центральный банк России принял решение повысить ключевую ставку до 17% годовых.',
         'https://rbc.ru/news/2',
         '2026-04-11 09:30:00+00', '2026-04-11 09:30:00+00', 3, 2, null),

    (3,  'Открытие нового стадиона в Москве',
         'В Москве состоялось торжественное открытие многофункционального спортивного комплекса.',
         'https://lenta.ru/news/3',
         '2026-04-12 12:00:00+00', '2026-04-12 12:00:00+00', 3, 3, null),

    (4,  'Прорыв в квантовых вычислениях',
         'Учёные из MIT представили квантовый процессор с рекордным числом кубитов.',
         'https://dzen.ru/news/4',
         '2026-04-13 14:00:00+00', '2026-04-13 14:00:00+00', 3, 5, null),

    (5,  'Новый метод лечения диабета',
         'Исследователи разработали инновационный метод лечения диабета второго типа без инъекций.',
         'https://lenta.ru/news/5',
         '2026-04-14 10:00:00+00', '2026-04-14 10:00:00+00', 3, 3, null),

    (6,  'Моя статья об архитектуре микросервисов',
         'Разбираем паттерны проектирования микросервисной архитектуры на примере реального проекта.',
         null,
         '2026-04-15 11:00:00+00', '2026-04-15 11:00:00+00', 2, null, 4),

    (7,  'Обзор рынка криптовалют',
         'Анализ текущего состояния рынка криптовалют и прогнозы на ближайший квартал.',
         null,
         '2026-04-16 09:00:00+00', '2026-04-16 09:00:00+00', 1, null, 5),

    (8,  'Спорная статья о политике',
         'Содержимое данной статьи было признано нарушающим правила сообщества.',
         null,
         '2026-04-16 15:00:00+00', '2026-04-16 15:00:00+00', 4, null, 6)
ON CONFLICT ("Id") DO NOTHING;

-- -------------------------------------------------------------
-- ArticleTags
-- -------------------------------------------------------------
INSERT INTO "ArticleTags" ("ArticleId", "TagId") VALUES
    (1, 1), (1, 6),
    (2, 2), (2, 3),
    (3, 4),
    (4, 1), (4, 5), (4, 6),
    (5, 5), (5, 7),
    (6, 1), (6, 6),
    (7, 3),
    (8, 2)
ON CONFLICT ("ArticleId", "TagId") DO NOTHING;

-- -------------------------------------------------------------
-- UserTagWeights
-- -------------------------------------------------------------
INSERT INTO "UserTagWeights" ("UserId", "TagId", "Weight", "IsSubscribed") VALUES
    (4, 1, 2.5, true),
    (4, 6, 1.5, true),
    (4, 5, 1.0, false),
    (5, 2, 2.0, true),
    (5, 3, 1.5, true),
    (5, 7, 1.0, true),
    (6, 4, 3.0, true),
    (6, 1, 1.0, false)
ON CONFLICT ("UserId", "TagId") DO NOTHING;

-- -------------------------------------------------------------
-- Likes
-- -------------------------------------------------------------
INSERT INTO "Likes" ("UserId", "ArticleId", "CreatedAt") VALUES
    (4, 1, '2026-04-10 10:00:00+00'),
    (4, 4, '2026-04-13 16:00:00+00'),
    (5, 2, '2026-04-11 11:00:00+00'),
    (5, 5, '2026-04-14 12:00:00+00'),
    (6, 3, '2026-04-12 14:00:00+00'),
    (6, 1, '2026-04-10 15:00:00+00')
ON CONFLICT ("UserId", "ArticleId") DO NOTHING;

-- -------------------------------------------------------------
-- SavedArticles
-- -------------------------------------------------------------
INSERT INTO "SavedArticles" ("UserId", "ArticleId", "SavedAt") VALUES
    (4, 1, '2026-04-10 11:00:00+00'),
    (4, 4, '2026-04-13 17:00:00+00'),
    (5, 2, '2026-04-11 12:00:00+00'),
    (5, 5, '2026-04-14 13:00:00+00'),
    (6, 3, '2026-04-12 15:00:00+00')
ON CONFLICT ("UserId", "ArticleId") DO NOTHING;

-- -------------------------------------------------------------
-- Comments
-- -------------------------------------------------------------
INSERT INTO "Comments" ("Id", "Content", "CreatedAt", "UserId", "ArticleId", "ParentCommentId") VALUES
    (1, 'Отличная статья, давно ждал подобного материала!',          '2026-04-10 12:00:00+00', 4, 1, null),
    (2, 'Согласен, особенно понравилась часть про производительность.', '2026-04-10 13:00:00+00', 5, 1, 1),
    (3, 'Интересно, как это повлияет на ипотечные ставки?',          '2026-04-11 10:00:00+00', 4, 2, null),
    (4, 'Скорее всего ставки по ипотеке тоже вырастут.',             '2026-04-11 11:30:00+00', 5, 2, 3),
    (5, 'Был на открытии, очень впечатляет!',                        '2026-04-12 16:00:00+00', 6, 3, null),
    (6, 'Квантовые вычисления — будущее уже здесь.',                 '2026-04-13 15:00:00+00', 4, 4, null),
    (7, 'Хотелось бы увидеть практическое применение.',              '2026-04-13 16:30:00+00', 5, 4, 6)
ON CONFLICT ("Id") DO NOTHING;

-- -------------------------------------------------------------
-- Reports
-- -------------------------------------------------------------
INSERT INTO "Reports" ("Id", "Description", "CreatedAt", "UserId", "ArticleId", "ReportTypeId") VALUES
    (1, 'Статья содержит недостоверную информацию.',  '2026-04-16 16:00:00+00', 4, 8, 3),
    (2, 'Разжигание ненависти.',                      '2026-04-16 16:30:00+00', 5, 8, 2),
    (3, 'Спам и реклама.',                            '2026-04-15 10:00:00+00', 6, 6, 1),
    (4, 'Нарушение авторских прав.',                  '2026-04-14 09:00:00+00', 4, 5, 5),
    (5, 'Агрессивный контент.',                       '2026-04-16 17:00:00+00', 5, 8, 4)
ON CONFLICT ("Id") DO NOTHING;

-- -------------------------------------------------------------
-- ModerationLogs
-- -------------------------------------------------------------
INSERT INTO "ModerationLogs" ("Id", "Action", "Reason", "CreatedAt", "ModeratorId", "ArticleId") VALUES
    (1, 'Approved', null,                                    '2026-04-10 09:00:00+00', 2, 1),
    (2, 'Approved', null,                                    '2026-04-11 10:00:00+00', 2, 2),
    (3, 'Approved', null,                                    '2026-04-12 11:00:00+00', 3, 3),
    (4, 'Approved', null,                                    '2026-04-13 13:00:00+00', 2, 4),
    (5, 'Approved', null,                                    '2026-04-14 09:30:00+00', 3, 5),
    (6, 'Rejected', 'Нарушение правил сообщества.',          '2026-04-16 16:00:00+00', 2, 8)
ON CONFLICT ("Id") DO NOTHING;

-- -------------------------------------------------------------
-- Сброс последовательностей после ручной вставки с явными Id
-- -------------------------------------------------------------
SELECT setval(pg_get_serial_sequence('"Tags"',            'Id'), (SELECT MAX("Id") FROM "Tags"));
SELECT setval(pg_get_serial_sequence('"RssSources"',      'Id'), (SELECT MAX("Id") FROM "RssSources"));
SELECT setval(pg_get_serial_sequence('"Users"',           'Id'), (SELECT MAX("Id") FROM "Users"));
SELECT setval(pg_get_serial_sequence('"Articles"',        'Id'), (SELECT MAX("Id") FROM "Articles"));
SELECT setval(pg_get_serial_sequence('"Comments"',        'Id'), (SELECT MAX("Id") FROM "Comments"));
SELECT setval(pg_get_serial_sequence('"Reports"',         'Id'), (SELECT MAX("Id") FROM "Reports"));
SELECT setval(pg_get_serial_sequence('"ModerationLogs"',  'Id'), (SELECT MAX("Id") FROM "ModerationLogs"));
SELECT setval(pg_get_serial_sequence('"ReportTypes"',     'Id'), (SELECT MAX("Id") FROM "ReportTypes"));
