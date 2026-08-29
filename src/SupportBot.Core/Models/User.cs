namespace SupportBot.Core.Models;

public class User
{
    public int Id { get; set; }
    public long TelegramId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
