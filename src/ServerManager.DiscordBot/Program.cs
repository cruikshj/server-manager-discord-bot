using Discord.Interactions;
using Discord.WebSocket;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddYamlFile("appsettings.yaml", optional: true);
builder.Configuration.AddYamlFile("appsettings.yml", optional: true);
builder.Configuration.AddDirectory("Config");

builder.Configuration.AddEnvironmentVariables("SERVERMANAGER_");

builder.Services.AddOptions<AppSettings>().Bind(builder.Configuration);
builder.Services.ConfigureOptions<AppSettingsSetup>();

builder.Services.AddMemoryCache();

builder.Services.AddSingleton(sp => {
    return new DiscordSocketClient(new DiscordSocketConfig
    {
        GatewayIntents = Discord.GatewayIntents.None
    });
});
builder.Services.AddSingleton<InteractionService>();
builder.Services.AddHostedService<BotService>();

builder.Services.AddSingleton<ServerManager>();
builder.ConfigureServerInfoProviders();
builder.ConfigureServerHostAdapters();
builder.ConfigureLargeFileDownloadHandler();

var app = builder.Build();

var lfdHandler = app.Services.GetService<ILargeFileDownloadHandler>();
if (lfdHandler is not null)
{
    lfdHandler.MapEndpoints(app);
}

app.Run();
