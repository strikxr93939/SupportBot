using SupportBot.Core.Models;

namespace SupportBot.Core.Services;

public static class KnowledgeSearch
{
    private static readonly HashSet<string> StopWords = new()
    {
        "как", "что", "где", "когда", "почему", "зачем", "можно", "нужно", "мне", "это", "для", "или", "если",
        "и", "в", "на", "с", "по", "за", "из", "у", "к", "о", "а", "не", "то", "же", "бы", "ли", "есть", "моя", "мой",
        "the", "a", "an", "is", "are", "how", "what", "where", "to", "of", "in", "on", "for", "my", "i", "do"
    };

    public static IReadOnlyList<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        var cleaned = new string(text.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray());

        return cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 2 && !StopWords.Contains(t))
            .Distinct()
            .ToList();
    }

    public static double Score(string query, Knowledge item)
    {
        var queryTokens = Normalize(Tokenize(query));
        if (queryTokens.Count == 0) return 0;

        var questionTokens = Normalize(Tokenize(item.Question)).ToHashSet();
        var tagTokens = Normalize(Tokenize(item.Tags)).ToHashSet();

        double matched = queryTokens.Count(t => questionTokens.Contains(t));
        double score = matched / queryTokens.Count;

        if (queryTokens.Any(t => tagTokens.Contains(t))) score += 0.5;

        var flat = query.Trim().ToLowerInvariant();
        if (flat.Length >= 5 && item.Question.ToLowerInvariant().Contains(flat)) score += 0.3;

        return score;
    }

    private static readonly string[] Suffixes =
    {
        "ами", "ями", "ешь", "ует", "ая", "яя", "ов", "ев", "ей", "ой", "ем", "ом",
        "ам", "ах", "ях", "ть", "ла", "ло", "ли", "у", "ю", "а", "я", "ы", "и", "е", "о", "ь"
    };

    private static IReadOnlyList<string> Normalize(IReadOnlyList<string> tokens) =>
        tokens.Select(NormalizeToken).Distinct().ToList();

    private static string NormalizeToken(string token)
    {
        foreach (var suffix in Suffixes)
        {
            if (token.Length - suffix.Length >= 3 && token.EndsWith(suffix, StringComparison.Ordinal))
                return token[..^suffix.Length];
        }
        return token;
    }

    public static Knowledge? BestMatch(string query, IEnumerable<Knowledge> items, double threshold = 0.34)
    {
        return items
            .Select(item => (Item: item, Score: Score(query, item)))
            .Where(x => x.Score >= threshold)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Item)
            .FirstOrDefault();
    }
}
