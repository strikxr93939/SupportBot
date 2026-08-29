using Microsoft.Extensions.Configuration;

namespace SupportBot.Core.Configuration;

public enum LlmProvider
{
    OpenAi,
    Ollama
}

public sealed class BotConfiguration
{
    public required string BotToken { get; init; }
    public string? WebhookUrl { get; init; }
    public LlmProvider Provider { get; init; }
    public required string LlmEndpoint { get; init; }
    public string? LlmApiKey { get; init; }
    public required string LlmModel { get; init; }
    public int ContextMessages { get; init; } = 3;
    public string DbPath { get; init; } = "supportbot.db";

    public static BotConfiguration FromConfiguration(IConfiguration configuration)
    {
        var provider = (configuration["LLM:Provider"] ?? "ollama").Equals("openai", StringComparison.OrdinalIgnoreCase)
            ? LlmProvider.OpenAi
            : LlmProvider.Ollama;

        var endpoint = provider == LlmProvider.OpenAi
            ? configuration["LLM:OpenAiEndpoint"] ?? "https://api.openai.com/v1/chat/completions"
            : configuration["LLM:OllamaEndpoint"] ?? "http://localhost:11434/api/chat";

        var botToken = configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
            throw new InvalidOperationException(
                "Telegram:BotToken не задан (appsettings.json или переменная окружения Telegram__BotToken)");

        return new BotConfiguration
        {
            BotToken = botToken,
            WebhookUrl = configuration["Telegram:WebhookUrl"],
            Provider = provider,
            LlmEndpoint = endpoint,
            LlmApiKey = configuration["LLM:ApiKey"],
            LlmModel = configuration["LLM:Model"] ?? (provider == LlmProvider.OpenAi ? "gpt-4o-mini" : "llama3.1"),
            ContextMessages = int.TryParse(configuration["LLM:ContextMessages"], out var n) && n > 0 ? n : 3,
            DbPath = configuration["Database:Path"] ?? "supportbot.db"
        };
    }
}
