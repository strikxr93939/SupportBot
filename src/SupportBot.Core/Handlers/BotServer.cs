using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SupportBot.Core.Configuration;
using SupportBot.Core.Handlers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace SupportBot.Core.Handlers;

public sealed class BotServer(BotConfiguration config, MessageHandler handler, ILogger<BotServer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bot = new TelegramBotClient(config.BotToken, cancellationToken: stoppingToken);
        var me = await bot.GetMe(stoppingToken);

        if (config.WebhookUrl is { } webhookUrl)
        {
            await bot.SetWebhook(webhookUrl, cancellationToken: stoppingToken);
            logger.LogInformation("Бот @{Username}: webhook установлен на {Url}, polling отключён", me.Username, webhookUrl);
            await Task.Delay(Timeout.Infinite, stoppingToken);
            return;
        }

        await bot.DeleteWebhook(cancellationToken: stoppingToken);
        logger.LogInformation("Бот @{Username} запущен в режиме long-polling", me.Username);

        bot.StartReceiving(
            (client, update, token) => handler.HandleUpdateAsync(client, update, token),
            (_, exception, _) =>
            {
                logger.LogError(exception, "Ошибка при получении обновлений");
                return Task.CompletedTask;
            },
            new ReceiverOptions { AllowedUpdates = [UpdateType.Message] },
            stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
