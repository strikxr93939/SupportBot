using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SupportBot.Core.Configuration;
using SupportBot.Core.Data;
using SupportBot.Core.Services;
using Telegram.Bot;
using TgUser = Telegram.Bot.Types.User;

namespace SupportBot.Core.Handlers;

public sealed class MessageHandler(
    IServiceScopeFactory scopeFactory,
    BotConfiguration config,
    ILogger<MessageHandler> logger)
{
    public async Task HandleUpdateAsync(ITelegramBotClient bot, global::Telegram.Bot.Types.Update update, CancellationToken ct)
    {
        try
        {
            if (update.Message is not { } message || message.Text is not { } text) return;

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            var knowledge = scope.ServiceProvider.GetRequiredService<KnowledgeService>();
            var llm = scope.ServiceProvider.GetRequiredService<ILLMService>();

            var user = await EnsureUserAsync(db, message.From!, ct);
            var chatId = message.Chat.Id;

            if (text.StartsWith('/'))
            {
                await HandleCommandAsync(bot, db, user, chatId, text, ct);
                return;
            }

            var reply = await BuildReplyAsync(db, knowledge, llm, user, text, ct);

            db.Dialogs.Add(new Models.Dialog
            {
                UserId = user.Id,
                MessageText = text,
                Response = reply,
                Timestamp = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);

            await bot.SendMessage(chatId, reply, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось обработать обновление");
        }
    }

    private async Task HandleCommandAsync(ITelegramBotClient bot, BotDbContext db, Models.User user, long chatId, string text, CancellationToken ct)
    {
        var command = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();

        switch (command)
        {
            case "/start":
                await bot.SendMessage(chatId, ReplyBuilder.Greeting(text.Split(' ', 2) is [_, string name] ? name : null), cancellationToken: ct);
                break;

            case "/history":
            {
                var dialogs = await db.Dialogs.AsNoTracking()
                    .Where(d => d.UserId == user.Id)
                    .OrderByDescending(d => d.Timestamp)
                    .Take(5)
                    .ToListAsync(ct);
                await bot.SendMessage(chatId, ReplyBuilder.History(dialogs), cancellationToken: ct);
                break;
            }

            case "/stats":
            {
                var questions = await db.Dialogs.CountAsync(d => d.UserId == user.Id, ct);
                var answers = await db.Dialogs.CountAsync(d => d.UserId == user.Id && d.Response != null, ct);
                await bot.SendMessage(chatId, ReplyBuilder.Stats(questions, answers), cancellationToken: ct);
                break;
            }

            default:
                await bot.SendMessage(chatId, "Неизвестная команда. Доступны: /start, /history, /stats", cancellationToken: ct);
                break;
        }
    }

    private async Task<string> BuildReplyAsync(
        BotDbContext db, KnowledgeService knowledge, ILLMService llm, Models.User user, string text, CancellationToken ct)
    {
        var kbAnswer = await knowledge.SearchAnswerAsync(text, ct);
        if (kbAnswer is not null)
            return ReplyBuilder.FromKnowledge(kbAnswer);

        var history = await db.Dialogs.AsNoTracking()
            .Where(d => d.UserId == user.Id)
            .OrderByDescending(d => d.Timestamp)
            .Take(config.ContextMessages)
            .ToListAsync(ct);

        try
        {
            return ReplyBuilder.FromLlm(await llm.AskAsync(text, history, ct));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LLM недоступен ({Provider})", config.Provider);
            return ReplyBuilder.Error();
        }
    }

    private static async Task<Models.User> EnsureUserAsync(BotDbContext db, TgUser from, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.TelegramId == from.Id, ct);
        if (user is null)
        {
            user = new Models.User { TelegramId = from.Id, CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
        }
        return user;
    }
}
