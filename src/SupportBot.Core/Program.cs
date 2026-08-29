using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SupportBot.Core.Configuration;
using SupportBot.Core.Data;
using SupportBot.Core.Handlers;
using SupportBot.Core.Services;

var builder = Host.CreateApplicationBuilder(args);

var config = BotConfiguration.FromConfiguration(builder.Configuration);

builder.Services.AddSingleton(config);
builder.Services.AddDbContext<BotDbContext>(options => options.UseSqlite($"Data Source={config.DbPath}"));
builder.Services.AddScoped<KnowledgeService>();
builder.Services.AddHttpClient<ILLMService, LLMService>((sp, http) => http.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddSingleton<MessageHandler>();
builder.Services.AddHostedService<BotServer>();

using var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
    db.Database.EnsureCreated();
    KnowledgeSeeder.Seed(db);
}

await host.RunAsync();
