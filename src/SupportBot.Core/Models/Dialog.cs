namespace SupportBot.Core.Models;

public class Dialog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public string? Response { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
