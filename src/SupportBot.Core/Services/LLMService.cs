using System.Text;
using System.Text.Json;
using SupportBot.Core.Configuration;
using SupportBot.Core.Models;

namespace SupportBot.Core.Services;

public interface ILLMService
{
    Task<string> AskAsync(string question, IReadOnlyList<Dialog> history, CancellationToken ct = default);
}

public sealed class LLMService(HttpClient http, BotConfiguration config) : ILLMService
{
    public async Task<string> AskAsync(string question, IReadOnlyList<Dialog> history, CancellationToken ct = default)
    {
        var messages = new List<object>
        {
            new { role = "system", content = "Ты — вежливый бот поддержки компании. Отвечай кратко, по делу и на русском языке." }
        };

        foreach (var dialog in history.OrderBy(d => d.Timestamp))
        {
            messages.Add(new { role = "user", content = dialog.MessageText });
            if (!string.IsNullOrWhiteSpace(dialog.Response))
                messages.Add(new { role = "assistant", content = dialog.Response });
        }

        messages.Add(new { role = "user", content = question });

        object body = config.Provider == LlmProvider.Ollama
            ? new { model = config.LlmModel, messages, stream = false }
            : new { model = config.LlmModel, messages };

        using var request = new HttpRequestMessage(HttpMethod.Post, config.LlmEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        return config.Provider == LlmProvider.Ollama
            ? doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? string.Empty
            : doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }
}
