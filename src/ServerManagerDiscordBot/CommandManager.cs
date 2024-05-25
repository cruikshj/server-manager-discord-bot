using System.Reflection;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Options;

public class CommandManager(
    IOptions<AppSettings> appSettings,
    InteractionService interactionService,
    IServiceProvider serviceProvider)
{
    public AppSettings AppSettings { get; } = appSettings.Value;
    public InteractionService InteractionService { get; } = interactionService;
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public IReadOnlyCollection<IApplicationCommand> Commands { get; private set; } = [];

    public IApplicationCommand GetCommand(string name)
    {
        var command = Commands.FirstOrDefault(c => c.Name == name);
        if (command is null)
        {
            throw new ArgumentException($"Command '{name}' not found.", nameof(name));
        }
        return command;
    }

    public async Task RegisterCommandsAsync()
    {        
        await InteractionService.AddModulesAsync(Assembly.GetEntryAssembly(), ServiceProvider);
        
        if (!AppSettings.GuildIds.Any())
        {            
            Commands = await InteractionService.RegisterCommandsGloballyAsync(true);
        }
        else
        {
            await InteractionService.RestClient.DeleteAllGlobalCommandsAsync();
            foreach (var guildId in AppSettings.GuildIds)
            {
                Commands = await InteractionService.RegisterCommandsToGuildAsync(guildId, true);
            }
        }
    }
}