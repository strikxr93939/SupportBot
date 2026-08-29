using SupportBot.Core.Models;

namespace SupportBot.Core.Services;

public static class ReplyBuilder
{
    private const int TruncateLength = 60;

    public static string Greeting(string? name) =>
        $"Привет, {(string.IsNullOrWhiteSpace(name) ? "друг" : name)}!" +
        "\nЯ бот поддержки. Задайте вопрос — сначала поищу ответ в базе знаний, если не найду, спрошу у LLM." +
        "\n\nКоманды:" +
        "\n/history — последние диалоги" +
        "\n/stats — статистика вопросов и ответов";

    public static string FromKnowledge(string answer) => $"[База знаний]\n{answer}";

    public static string FromLlm(string answer) => $"[Ответ от LLM]\n{answer}";

    public static string Error() => "Не удалось получить ответ. Попробуйте позже или переформулируйте вопрос.";

    public static string History(IEnumerable<Dialog> dialogs)
    {
        var list = dialogs.ToList();
        if (list.Count == 0) return "История пуста.";

        var sb = new System.Text.StringBuilder("Последние диалоги:");
        foreach (var d in list.OrderBy(d => d.Timestamp))
        {
            sb.AppendLine();
            sb.Append($"[{d.Timestamp:dd.MM.yyyy HH:mm}] ");
            sb.Append(Truncate(d.MessageText));
            if (!string.IsNullOrWhiteSpace(d.Response))
            {
                sb.Append(" -> ");
                sb.Append(Truncate(d.Response));
            }
        }
        return sb.ToString();
    }

    public static string Stats(int questions, int answers) =>
        $"Статистика:\nВопросов: {questions}\nОтветов: {answers}";

    private static string Truncate(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty
        : text.Length <= TruncateLength ? text
        : text[..TruncateLength] + "...";
}
