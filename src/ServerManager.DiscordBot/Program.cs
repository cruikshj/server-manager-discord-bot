using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables("SERVERMANAGER_");

builder.Services.AddOptions<AppSettings>()
    .Bind(builder.Configuration);

builder.Services.AddMemoryCache();
builder.Services.AddTransient<IContentTypeProvider, FileExtensionContentTypeProvider>();
builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddSingleton<InteractionService>();
builder.Services.AddSingleton<ServerManager>();
builder.Services.AddSingleton<KubernetesClient>();
builder.Services.AddHostedService<BotService>();

var app = builder.Build();

app.MapEndpoints();

app.Run();
