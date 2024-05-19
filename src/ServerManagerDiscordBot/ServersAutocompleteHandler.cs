using Discord;
using Discord.Interactions;

public class ServersAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context, 
        IAutocompleteInteraction autocompleteInteraction, 
        IParameterInfo parameter, 
        IServiceProvider services)
    {
        var serverManager = services.GetRequiredService<ServerManager>();

        var servers = await serverManager.GetServersAsync();

        var serverNames = servers.Keys;
        
        var value = autocompleteInteraction.Data.Current.Value as string;
        if (!string.IsNullOrWhiteSpace(value))
        {
            serverNames = serverNames.Where(s => s.Contains(value.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        // max - 25 suggestions at a time (API limit)
        var results = serverNames.Select(s => new AutocompleteResult(s, s)).Take(25);

        return AutocompletionResult.FromSuccess(results);
    }
}