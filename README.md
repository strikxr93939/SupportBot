# SupportBot

Telegram-бот поддержки: отвечает по базе знаний (EF Core + SQLite), а если подходящего ответа нет — обращается к LLM (OpenAI или локальный Ollama) с контекстом последних сообщений. История диалогов сохраняется.

## Стек

- .NET 8 (консольное приложение, Generic Host)
- Telegram.Bot 22.x (long-polling по умолчанию; webhook настраивается через конфигурацию)
- EF Core 8 + SQLite
- HttpClient → OpenAI `/v1/chat/completions` или Ollama `/api/chat`
- xUnit, Dockerfile, GitHub Actions (CI: restore → build → test)

## Команды бота

| Команда | Действие |
|---|---|
| `/start` | Регистрация пользователя, приветствие |
| текст | Поиск по базе знаний → если ничего не найдено, запрос к LLM |
| `/history` | Последние 5 диалогов пользователя |
| `/stats` | Количество вопросов и ответов |

Ответы помечаются префиксом `[База знаний]` или `[Ответ от LLM]`.

## Архитектура

```
BotServer (BackgroundService, long-polling)
  → MessageHandler (команды / диалоги, DI-scope на каждое обновление)
      → KnowledgeService (EF) → KnowledgeSearch (токенизация, стоп-слова,
        нормализация суффиксов, теги, точное вхождение; порог 0.34)
      → LLMService (HttpClient → OpenAI / Ollama, контекст последних 3 сообщений)
      → ReplyBuilder (форматирование ответов)
```

## Схема БД (EF Core, SQLite)

- `User { Id, TelegramId, CreatedAt }` (TelegramId уникален)
- `Dialog { Id, UserId, MessageText, Response, Timestamp }`
- `Knowledge { Id, Question, Answer, Tags }` (заполняется сидом при первом старте)

## Конфигурация

Через `appsettings.json` или переменные окружения (двойное подчёркивание — вложенность):

| Переменная | Назначение | По умолчанию |
|---|---|---|
| `Telegram__BotToken` | Токен бота (**обязателен**) | — |
| `Telegram__WebhookUrl` | Если задан — ставится webhook, polling отключается | — |
| `LLM__Provider` | `ollama` или `openai` | `ollama` |
| `LLM__Model` | Модель | `llama3.1` / `gpt-4o-mini` |
| `LLM__ApiKey` | Ключ OpenAI (Bearer) | — |
| `LLM__ContextMessages` | Сколько последних сообщений отправлять в контекст | `3` |
| `Database__Path` | Путь к SQLite-файлу | `supportbot.db` |

Эндпоинты LLM: OpenAI — `https://api.openai.com/v1/chat/completions`, Ollama — `http://localhost:11434/api/chat`.

## Запуск

```bash
# локально
export Telegram__BotToken="123456:ABC..."
dotnet run --project src/SupportBot.Core

# docker
docker build -t supportbot .
docker run -e Telegram__BotToken="123456:ABC..." supportbot
```

Бот готов к работе примерно за 2 минуты: создайте бота у @BotFather, вставьте токен, запустите.

## Тесты и CI

```bash
dotnet test
```

`tests/SupportBot.Tests/KnowledgeSearchTests.cs` покрывает поиск по базе знаний (стоп-слова, падежи, теги, порог нерелевантности). GitHub Actions (`.github/workflows/ci.yml`) гоняет restore → build → test при каждом push/PR.
