using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System;
using CodeQuest.Model;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;

namespace CodeQuest.Context
{
    public class LogContext : DbContext
    {
        public DbSet<Log> Log { get; set; }

        public LogContext()
        {
            Database.EnsureCreated();
            Log.Load();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("server=127.0.0.1;uid=root;pwd=;database=CodeQuest",
                new MySqlServerVersion(new Version(8, 0, 11)));
        }
    }
    /*

--
-- База данных: `CodeQuest`
--

-- --------------------------------------------------------

--
-- Структура таблицы `Admins`
--

CREATE TABLE `Admins` (
  `id` int (11) NOT NULL,
  `login` varchar(100) NOT NULL,
  `password` varchar(255) NOT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Дамп данных таблицы `Admins`
--

INSERT INTO `Admins` (`id`, `login`, `password`, `created_at`) VALUES
(1, 'admin', 'admin123', '2026-01-28 12:20:52');

-- --------------------------------------------------------

--
-- Структура таблицы `Log`
--

CREATE TABLE `Log` (
  `id` int (11) NOT NULL,
  `idUser` int (11) NOT NULL,
  `whatDo` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_At` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE = utf8mb4_unicode_ci;

--
-- Дамп данных таблицы `Log`
--

INSERT INTO `Log` (`id`, `idUser`, `whatDo`, `created_At`) VALUES
(1, 9, 'Полльзователь с Id 9 прошёл регистрацию.\n', '2026-01-29 15:01:00'),
(2, 10, 'Полльзователь с Id 10 прошёл регистрацию.\n', '2026-01-29 15:01:23'),
(3, 5, 'Полльзователь с Id 5 вошёл в приложение.', '2026-01-29 15:01:40');

-- --------------------------------------------------------

--
-- Структура таблицы `Quiz`
--

CREATE TABLE `Quiz` (
  `id` int (11) NOT NULL,
  `topic_id` int (11) NOT NULL,
  `question_text` text NOT NULL,
  `options` json NOT NULL,
  `correct_answer` varchar(255) NOT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Дамп данных таблицы `Quiz`
--

INSERT INTO `Quiz` (`id`, `topic_id`, `question_text`, `options`, `correct_answer`, `created_at`) VALUES
(1, 1, 'Что такое переменная?', '[\"Контейнер для данных\", \"Тип данных\", \"Функция\", \"Цикл\"]', 'Контейнер для данных', '2026-01-28 12:20:53');

-- --------------------------------------------------------

--
-- Структура таблицы `Topics`
--

CREATE TABLE `Topics` (
  `id` int (11) NOT NULL,
  `title` varchar(255) NOT NULL,
  `short_description` text,
  `full_description` text,
  `order_index` int(11) DEFAULT '0',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Дамп данных таблицы `Topics`
--

INSERT INTO `Topics` (`id`, `title`, `short_description`, `full_description`, `order_index`, `created_at`) VALUES
(1, 'Основы программирования', 'Введение в программирование', 'Полное руководство по основам программирования', 1, '2026-01-28 12:20:53');

-- --------------------------------------------------------

--
-- Структура таблицы `UserProgress`
--

CREATE TABLE `UserProgress` (
  `id` int(11) NOT NULL,
  `user_id` int(11) NOT NULL,
  `topic_id` int(11) NOT NULL,
  `is_completed` tinyint(1) DEFAULT '0',
  `score` int(11) DEFAULT '0',
  `total_questions` int(11) NOT NULL DEFAULT '5',
  `completed_at` datetime DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Дамп данных таблицы `UserProgress`
--

INSERT INTO `UserProgress` (`id`, `user_id`, `topic_id`, `is_completed`, `score`, `total_questions`, `completed_at`, `created_at`) VALUES
(1, 1, 1, 1, 4, 5, NULL, '2026-01-28 12:20:53');

-- --------------------------------------------------------

--
-- Структура таблицы `Users`
--

CREATE TABLE `Users` (
  `id` int(11) NOT NULL,
  `username` varchar(100) NOT NULL,
  `email` varchar(255) NOT NULL,
  `passwordhash` varchar(255) NOT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ProfileIconFileName` varchar(255) DEFAULT NULL,
  `IsIconGenerated` tinyint(1) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Дамп данных таблицы `Users`
--

INSERT INTO `Users` (`id`, `username`, `email`, `passwordhash`, `created_at`, `ProfileIconFileName`, `IsIconGenerated`) VALUES
(1, 'testuser', 'test@example.com', 'ER6DORbQk4Q/5tlGOMDPdZqy6lH6vRlN4hFF7B4d3RE=', '2026-01-28 12:20:53', NULL, NULL),
(2, 'sdfg', 'dsfgdsfg', 'PR0xPxYUlF3GEAIr6mmMImZIr+CmmyGJWJVzV7VeQO0=', '2026-01-28 14:58:18', 'image/png', NULL),
(3, 'user', 'user', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', '2026-01-28 15:59:22', NULL, 0),
(4, 'nastya', 'nastya', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', '2026-01-28 16:02:02', 'image/png', 1),
(5, 'qweqwe', 'qweqwe', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', '2026-01-28 18:26:28', 'avatar_5_20260128182708.png', 1),
(6, 'nastya228', 'nastya228', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', '2026-01-29 11:06:01', NULL, 0),
(7, 'sdfsdf', 'sdfsdfsd', '8sSng8meq06HMq2kJPqcWJtc0o4VwnF1/VB8G2GsW8c=', '2026-01-29 11:12:20', 'avatar_7_20260129111251.png', 1),
(8, 'sva', 'sva', 'FuST8UssaPUzl8GW0lYfhbP1ofD4BEZOCNryO3cHiGQ=', '2026-01-29 14:43:54', NULL, 0),
(9, 'qwe', 'qwe', 'SJzV28cIx+VB3k182Rzm0PFhNXO3/FtA05Qsy5VVzzU=', '2026-01-29 15:01:00', 'avatar_9_20260129150128.png', 1),
(10, 'qweee', 'qwee', 'SJzV28cIx+VB3k182Rzm0PFhNXO3/FtA05Qsy5VVzzU=', '2026-01-29 15:01:23', NULL, 0);

--
-- Индексы сохранённых таблиц
--

--
-- Индексы таблицы `Admins`
--
ALTER TABLE `Admins`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `login` (`login`);

--
-- Индексы таблицы `Log`
--
ALTER TABLE `Log`
  ADD PRIMARY KEY (`id`);

--
-- Индексы таблицы `Quiz`
--
ALTER TABLE `Quiz`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_topic` (`topic_id`);

--
-- Индексы таблицы `Topics`
--
ALTER TABLE `Topics`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_order` (`order_index`);

--
-- Индексы таблицы `UserProgress`
--
ALTER TABLE `UserProgress`
  ADD PRIMARY KEY (`id`),
  ADD KEY `topic_id` (`topic_id`),
  ADD KEY `idx_user_topic` (`user_id`,`topic_id`);

--
-- Индексы таблицы `Users`
--
ALTER TABLE `Users`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `email` (`email`);

--
-- AUTO_INCREMENT для сохранённых таблиц
--

--
-- AUTO_INCREMENT для таблицы `Admins`
--
ALTER TABLE `Admins`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT для таблицы `Log`
--
ALTER TABLE `Log`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT для таблицы `Quiz`
--
ALTER TABLE `Quiz`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT для таблицы `Topics`
--
ALTER TABLE `Topics`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT для таблицы `UserProgress`
--
ALTER TABLE `UserProgress`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT для таблицы `Users`
--
ALTER TABLE `Users`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=11;

--
-- Ограничения внешнего ключа сохраненных таблиц
--

--
-- Ограничения внешнего ключа таблицы `Quiz`
--
ALTER TABLE `Quiz`
  ADD CONSTRAINT `quiz_ibfk_1` FOREIGN KEY (`topic_id`) REFERENCES `Topics` (`id`) ON DELETE CASCADE;

--
-- Ограничения внешнего ключа таблицы `UserProgress`
--
ALTER TABLE `UserProgress`
  ADD CONSTRAINT `userprogress_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `Users` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `userprogress_ibfk_2` FOREIGN KEY (`topic_id`) REFERENCES `Topics` (`id`) ON DELETE CASCADE;
COMMIT;
*/
}
