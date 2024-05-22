using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

public class BotService(
    IOptions<AppSettings> appSettings,
    DiscordSocketClient client,
    CommandManager commandManager,
    InteractionService interactionService,
    ILogger<BotService> logger,
    IServiceProvider serviceProvider) 
    : IHostedService, IDisposable
{
    public AppSettings AppSettings { get; } = appSettings.Value;
    public InteractionService InteractionService { get; } = interactionService;
    public ILogger Logger { get; } = logger;
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    public DiscordSocketClient Client { get; } = client;
    public CommandManager CommandManager { get; } = commandManager;
    public IReadOnlyCollection<IApplicationCommand> Commands { get; private set; } = [];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Client.Log += LogAsync;
        InteractionService.Log += LogAsync;

        Client.Ready += Ready;

        Client.ButtonExecuted += CommandHandler;
        Client.SlashCommandExecuted += CommandHandler;
        Client.SelectMenuExecuted += CommandHandler;
        Client.AutocompleteExecuted += CommandHandler;

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
        await CommandManager.RegisterCommandsAsync();

        await Client.SetStatusAsync(UserStatus.Online);
    }

    private async Task CommandHandler(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(Client, interaction);
        await InteractionService.ExecuteCommandAsync(context, ServiceProvider);
    }

    private Task LogAsync(LogMessage message)
    {
        Logger.Log(
            logLevel: message.Severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Trace,
                LogSeverity.Debug => LogLevel.Debug,
                _ => LogLevel.Information
            },
            message: message.Message,
            exception: message.Exception,
            eventId: new EventId(0));
         return Task.CompletedTask;
    }
}