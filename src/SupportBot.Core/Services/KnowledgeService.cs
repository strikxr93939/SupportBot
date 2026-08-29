using Microsoft.EntityFrameworkCore;
using SupportBot.Core.Data;
using SupportBot.Core.Models;

namespace SupportBot.Core.Services;

public sealed class KnowledgeService(BotDbContext db)
{
    public async Task<string?> SearchAnswerAsync(string question, CancellationToken ct = default)
    {
        var items = await db.Knowledge.AsNoTracking().ToListAsync(ct);
        return KnowledgeSearch.BestMatch(question, items)?.Answer;
    }

    public async Task<Knowledge> AddAsync(string question, string answer, string? tags = null, CancellationToken ct = default)
    {
        var knowledge = new Knowledge { Question = question, Answer = answer, Tags = tags };
        db.Knowledge.Add(knowledge);
        await db.SaveChangesAsync(ct);
        return knowledge;
    }
}
