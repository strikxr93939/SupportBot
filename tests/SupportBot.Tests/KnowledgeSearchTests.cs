using Xunit;
using SupportBot.Core.Models;
using SupportBot.Core.Services;

namespace SupportBot.Tests;

public class KnowledgeSearchTests
{
    private static readonly Knowledge[] KnowledgeBase =
    {
        new() { Question = "Как сбросить пароль?", Answer = "Нажмите 'Забыли пароль?'.", Tags = "пароль;вход;аккаунт" },
        new() { Question = "Как оплатить подписку?", Answer = "Оплата в разделе 'Аккаунт'.", Tags = "оплата;подписка" },
        new() { Question = "В какое время работает поддержка?", Answer = "С 9:00 до 21:00 МСК.", Tags = "поддержка;график" }
    };

    [Fact]
    public void TokenizeRemovesStopWordsAndShortTokens()
    {
        var tokens = KnowledgeSearch.Tokenize("Как мне сбросить мой пароль на a б");

        Assert.Equal(new[] { "сбросить", "пароль" }, tokens);
    }

    [Fact]
    public void BestMatchFindsPasswordQuestion()
    {
        var best = KnowledgeSearch.BestMatch("не могу войти, забыл пароль", KnowledgeBase);

        Assert.NotNull(best);
        Assert.Contains("Забыли пароль", best!.Answer);
    }

    [Fact]
    public void BestMatchReturnsNullForUnrelatedQuery()
    {
        var best = KnowledgeSearch.BestMatch("рецепт борща со сметаной", KnowledgeBase);

        Assert.Null(best);
    }

    [Fact]
    public void BestMatchReturnsNullForEmptyQuery()
    {
        Assert.Null(KnowledgeSearch.BestMatch("", KnowledgeBase));
        Assert.Null(KnowledgeSearch.BestMatch("   ", KnowledgeBase));
        Assert.Null(KnowledgeSearch.BestMatch("как и в", KnowledgeBase));
    }

    [Fact]
    public void TagMatchBoostsScore()
    {
        var withTag = new Knowledge { Question = "Что делать при блокировке?", Answer = "Обратитесь в поддержку.", Tags = "пароль;блокировка" };
        var withoutTag = new Knowledge { Question = "Что делать при блокировке?", Answer = "Обратитесь в поддержку.", Tags = "аккаунт" };

        var query = "проблема с паролем";

        Assert.True(
            KnowledgeSearch.Score(query, withTag) > KnowledgeSearch.Score(query, withoutTag),
            "Совпадение по тегу должно увеличивать score");
    }

    [Fact]
    public void ExactSubstringMatchScoresHighest()
    {
        var best = KnowledgeSearch.BestMatch("как сбросить пароль", KnowledgeBase);

        Assert.NotNull(best);
        Assert.Equal("Как сбросить пароль?", best!.Question);
    }
}
