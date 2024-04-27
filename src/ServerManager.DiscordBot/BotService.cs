using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

public class BotService(
    IOptions<AppSettings> appSettings,
    DiscordSocketClient client,
    InteractionService interactionService,
    IServiceProvider serviceProvider) 
    : IHostedService, IDisposable
{
    public AppSettings AppSettings { get; } = appSettings.Value;
    public InteractionService InteractionService { get; } = interactionService;
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    public DiscordSocketClient Client { get; } = client;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Client.Log += LogAsync;
        InteractionService.Log += LogAsync;

        Client.Ready += Ready;

        Client.ButtonExecuted += CommandHandler;
        Client.SlashCommandExecuted += CommandHandler;
        Client.SelectMenuExecuted += CommandHandler;
        Client.AutocompleteExecuted += CommandHandler;

        await InteractionService.AddModulesAsync(Assembly.GetEntryAssembly(), ServiceProvider);

        await Client.LoginAsync(TokenType.Bot, AppSettings.BotToken);

        await Client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Client.SetStatusAsync(UserStatus.Offline);
        await Client.StopAsync();
        InteractionService.Log -= LogAsync;
        Client.Log -= LogAsync;
        Client.ButtonExecuted -= CommandHandler;
        Client.SlashCommandExecuted -= CommandHandler;
        Client.SelectMenuExecuted -= CommandHandler;
        Client.AutocompleteExecuted -= CommandHandler;
    }

    public void Dispose()
    {
        Client?.Dispose();
    }

    private async Task Ready()
    {
        await RegisterCommandsAsync();

        await Client.SetStatusAsync(UserStatus.Online);
    }

    private async Task CommandHandler(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(Client, interaction);
        await InteractionService.ExecuteCommandAsync(context, ServiceProvider);
    }

    private async Task RegisterCommandsAsync()
    {        
        if (!AppSettings.GuildIds.Any())
        {
            await InteractionService.RegisterCommandsGloballyAsync(true);
        }
        else
        {
            foreach (var guildId in AppSettings.GuildIds)
            {
                await InteractionService.RegisterCommandsToGuildAsync(guildId, true);
            }
        }
    }

    private Task LogAsync(LogMessage message)
    {
         Console.WriteLine($"[{message.Severity}] {message}");
         return Task.CompletedTask;
    }
}