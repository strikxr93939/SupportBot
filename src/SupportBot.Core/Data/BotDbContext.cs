using Microsoft.EntityFrameworkCore;
using SupportBot.Core.Models;

namespace SupportBot.Core.Data;

public class BotDbContext(DbContextOptions<BotDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Dialog> Dialogs => Set<Dialog>();
    public DbSet<Knowledge> Knowledge => Set<Knowledge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e => e.HasIndex(u => u.TelegramId).IsUnique());
        modelBuilder.Entity<Dialog>(e =>
        {
            e.Property(d => d.MessageText).IsRequired().HasMaxLength(4000);
            e.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId);
        });
        modelBuilder.Entity<Knowledge>(e =>
        {
            e.Property(k => k.Question).IsRequired().HasMaxLength(1000);
            e.Property(k => k.Answer).IsRequired().HasMaxLength(4000);
        });
    }
}
