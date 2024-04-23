using System.Text;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SmartFormat;

public class ServersCommandModule(ServerManager serverManager, IMemoryCache memoryCache, IOptions<AppSettings> appSettings) : InteractionModuleBase
{
    public ServerManager ServerManager { get; } = serverManager;
    public IMemoryCache MemoryCache { get; } = memoryCache;
    public AppSettings AppSettings { get; } = appSettings.Value;

    [SlashCommand("servers", "Display server information.")]
    public async Task Servers([Autocomplete(typeof(ServersAutocompleteHandler))]string? name = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            await List();
        }
        else
        {
            await Info(name);
        }
    }

    private async Task List()
    {
        var embed = new EmbedBuilder();
        embed.Title = "Server List";
        var description = new StringBuilder();
        var servers = await ServerManager.GetServersAsync();
        foreach (var server in servers)
        {
            description.AppendLine(server.Key);
        }
        embed.Description = description.ToString();

        await RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    private async Task Info(string name)
    {
        await DeferAsync(ephemeral: true);

        var info = await ServerManager.GetServerInfoAsync(name);
        var status = await ServerManager.GetServerStatusAsync(name);

        var embed = new EmbedBuilder();

        embed.Author = new EmbedAuthorBuilder();
        embed.Author.Name = name;

        if (!string.IsNullOrWhiteSpace(info.Game))
        {
            embed.Title = info.Game;
        }

        if (!string.IsNullOrWhiteSpace(info.Icon))
        {
            embed.ThumbnailUrl = info.Icon;
        }

        foreach (var field in info.Fields)
        {
            embed.AddField(field.Key, field.Value);
        }

        if (status is not null)
        {
            embed.AddField("Status", status);
        }

        embed.WithCurrentTimestamp();

        var component = new ComponentBuilder();
        var actionsRow = new ActionRowBuilder();
        component.WithRows([actionsRow]);
        if (!string.IsNullOrWhiteSpace(info.Deployment))
        {
            actionsRow
                .WithButton("Start", $"start|{name}", ButtonStyle.Success)
                .WithButton("Stop", $"stop|{name}", ButtonStyle.Primary)
                .WithButton("Logs", $"logs|{name}", ButtonStyle.Secondary);
        }
        if (!string.IsNullOrWhiteSpace(info.Readme))
        {
            actionsRow
                .WithButton("Readme", $"readme|{name}", ButtonStyle.Secondary);
        }
        if (!string.IsNullOrWhiteSpace(info.BackupsPath))
        {
            actionsRow
                .WithButton("Backups", $"backups|{name}", ButtonStyle.Secondary);
        }

        await FollowupAsync(embed: embed.Build(), components: component.Build(), ephemeral: true);
    }

    [ComponentInteraction("start|*")]
    public async Task Start(string name)
    {
        await RespondAsync($"The `{name}` server is starting...", ephemeral: true);
        try
        {
            await ServerManager.StartServerAsync(name, wait: true);

            await FollowupAsync($"{Context.User.GlobalName} started the `{name}` server.");
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    [ComponentInteraction("stop|*")]
    public async Task Stop(string name)
    {
        await RespondAsync($"The `{name}` server is stopping...", ephemeral: true);
        try
        {
            await ServerManager.StopServerAsync(name, wait: true); 

            await FollowupAsync($"{Context.User.GlobalName} stopped the `{name}` server.");
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    [ComponentInteraction("backups|*")]
    public async Task Backups(string name)
    {
        await DeferAsync(ephemeral: true);
        try
        {
            var files = (await ServerManager.GetServerBackupFilesAsync(name)).ToArray();

            if (files.Length == 0)
            {
                await FollowupAsync("No backup files found.", ephemeral: true);
                return;
            }

            files = files.OrderByDescending(f => f.Name).ToArray();

            var selectMenu = new SelectMenuBuilder();
            selectMenu.CustomId = $"backup|{name}";
            selectMenu.WithOptions(files.Select(f => new SelectMenuOptionBuilder() {
                Label = f.Name,
                Value = f.Name
            }).ToList());

            var component = new ComponentBuilder();
            var row = new ActionRowBuilder();
            component.WithRows([row]);
            row.WithSelectMenu(selectMenu);

            await FollowupAsync($"Select a `{name}` server backup file to download:", components: component.Build(), ephemeral: true);
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    [ComponentInteraction("backup|*")]
    public async Task DownloadBackup(string name, string fileName)
    {
        await DeferAsync(ephemeral: true);
        try
        {
            var fileInfo = await ServerManager.GetServerBackupFileAsync(name, fileName);

            if (fileInfo.Length < 25 * 1024 * 1024) // 25MB for Discord files
            {
                using (var stream = fileInfo.OpenRead())
                {
                    await FollowupWithFileAsync(stream, fileName, ephemeral: true);
                }
            }
            else if (AppSettings.DownloadHostUri is not null)
            {
                var downloadKey = Guid.NewGuid();
                MemoryCache.Set(downloadKey, fileInfo, AppSettings.DownloadCacheExpiration);

                var downloadUrl = new Uri(AppSettings.DownloadHostUri, $"/download/{downloadKey}");

                await FollowupAsync($"The file is too large to send directly. Download it from [here]({downloadUrl}).", ephemeral: true);
            }
            else
            {
                await FollowupAsync("The file is too large to send.", ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    [ComponentInteraction("readme|*")]
    public async Task Readme(string name)
    {
        await DeferAsync(ephemeral: true);
        try
        {
            var serverInfo = await ServerManager.GetServerInfoAsync(name);

            if (string.IsNullOrWhiteSpace(serverInfo.Readme))
            {
                await FollowupAsync("No readme found.", ephemeral: true);
                return;
            }

            var readme = Smart.Format(serverInfo.Readme, serverInfo).Trim();
            var quotedReadme = "> " + readme.Replace("\n", "\n> ");
            var response = $"Readme for the `{name}` server:";

            if (response.Length + quotedReadme.Length < 2000)
            {
                await FollowupAsync($"{response}\n{quotedReadme}", ephemeral: true);
            }
            else
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(readme)))
                {
                    await FollowupWithFileAsync(stream, "README.md", text: response, ephemeral: true);
                }
            }
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }
    
    [ComponentInteraction("logs|*")]
    public async Task Logs(string name)
    {
        await DeferAsync(ephemeral: true);
        try
        {
            var logs = await ServerManager.GetServerLogsAsync(name);

            if (!logs.Any())
            {
                await FollowupAsync($"No logs for the `{name}` server are available.", ephemeral: true);
                return;            
            }

            var fileAttachments = logs.Select(log => new FileAttachment(log.Value, $"{log.Key}.log")).ToArray();

            await FollowupWithFilesAsync(fileAttachments, text: $"Logs for the `{name}` server:", ephemeral: true);
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }
}