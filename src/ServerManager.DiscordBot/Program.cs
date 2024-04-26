using Discord.Interactions;
using Discord.WebSocket;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddConfigurationDirectory("Config");

builder.Configuration.AddEnvironmentVariables("SERVERMANAGER_");

builder.Services.AddOptions<AppSettings>().Bind(builder.Configuration);
builder.Services.ConfigureOptions<AppSettingsSetup>();

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddSingleton<InteractionService>();
builder.Services.AddHostedService<BotService>();

builder.Services.AddSingleton<ServerManager>();
builder.Services.AddSingleton<KubernetesClient>();

builder.ConfigureServerInfoProviders();
builder.ConfigureLargeFileDownloadHandler();

var app = builder.Build();

var lfdHandler = app.Services.GetService<ILargeFileDownloadHandler>();
if (lfdHandler is not null)
{
    lfdHandler.MapEndpoints(app);
}

app.Run();
