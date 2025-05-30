using System.Text;
using Discord;
using Discord.Interactions;
using Discord.Interactions.Builders;
using Microsoft.Extensions.Options;
using SmartFormat;

[Group("sm", "Commands for dedicated server management.")]
public class ServersCommandModule(
    IOptions<AppSettings> appSettings,
    CommandManager commandManager,
    ServerManager serverManager,
    ILargeFileDownloadHandler largeFileDownloadHandler,
    ILogger<ServersCommandModule> logger)
    : InteractionModuleBase
{
    public AppSettings AppSettings { get; } = appSettings.Value;
    public CommandManager CommandManager { get; } = commandManager;
    public ServerManager ServerManager { get; } = serverManager;
    public ILargeFileDownloadHandler LargeFileDownloadHandler { get; } = largeFileDownloadHandler;
    public ILogger Logger { get; } = logger;

    public override void Construct(ModuleBuilder builder, InteractionService commandService)
    {
        base.Construct(builder, commandService);

        builder.WithGroupName(AppSettings.SlashCommandPrefix);
    }

    [SlashCommand("list", "List all servers.")]
    public async Task List()
    {
        await DeferAsync(ephemeral: true);

        try
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

            await FollowupAsync(embed: embed.Build(), ephemeral: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing list command.");
            await FollowupAsync($"Interaction failed. See logs for details.", ephemeral: true);
        }
    }

    [SlashCommand("info", "Provides information and interactions for a server.")]
    public async Task Info([Autocomplete(typeof(ServersAutocompleteHandler))] string name)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            var info = await ServerManager.GetServerInfoAsync(name);

            ServerStatus? status = null;
            if (!string.IsNullOrWhiteSpace(info.HostAdapterName))
            {
                status = await ServerManager.GetServerStatusAsync(name);
            }

            var embed = BuildInfoEmbed(name, info, status);

            var component = BuildInfoComponent(name, info, status);

            await FollowupAsync(embed: embed, components: component, ephemeral: true);
        }
        catch (Exception ex)
        {
            if (string.IsNullOrEmpty(name))
            {
                Logger.LogError(ex, "Error processing servers command.");
            }
            else
            {
                Logger.LogError(ex, "Error processing servers command for server '{Name}'.", name);
            }
            await FollowupAsync($"Interaction failed. See logs for details.", ephemeral: true);
        }
    }

    [ComponentInteraction("refresh|*", true)]
    public async Task RefreshInfo(string name)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            var info = await ServerManager.GetServerInfoAsync(name);
            
            ServerStatus? status = null;
            if (!string.IsNullOrWhiteSpace(info.HostAdapterName))
            {
                status = await ServerManager.GetServerStatusAsync(name);
            }

            await ModifyOriginalResponseAsync(message =>
            {
                message.Embed = BuildInfoEmbed(name, info, status);
                message.Components = BuildInfoComponent(name, info, status);
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in status interaction for server '{Name}'.", name);
            await FollowupAsync($"Interaction failed. See logs for details.", ephemeral: true);
        }
    }

    private Embed BuildInfoEmbed(string name, ServerInfo info, ServerStatus? status)
    {
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

        if (status.HasValue)
        {
            embed.AddField("Status", status.Value.ToString());
        }

        embed.WithCurrentTimestamp();

        return embed.Build();
    }

    private MessageComponent BuildInfoComponent(string name, ServerInfo info, ServerStatus? status)
    {
        var component = new ComponentBuilder();

        if (!string.IsNullOrWhiteSpace(info.HostAdapterName))
        {
            var hostActionsRow = new ActionRowBuilder();
            hostActionsRow
                .WithButton("Refresh", $"refresh|{name}", ButtonStyle.Secondary)
                .WithButton("Start", $"start|{name}", ButtonStyle.Success)
                .WithButton("Restart", $"restart|{name}", ButtonStyle.Primary)
                .WithButton("Stop", $"stop|{name}", ButtonStyle.Danger)
                .WithButton("Logs", $"logs|{name}", ButtonStyle.Secondary);
            component.AddRow(hostActionsRow);
        }

        var contentActionsRow = new ActionRowBuilder();
        var includeContentActionsRow = false;

        if (!string.IsNullOrWhiteSpace(info.Readme))
        {
            contentActionsRow
                .WithButton("Readme", $"readme|{name}", ButtonStyle.Secondary);
            includeContentActionsRow = true;
        }
        if (AppSettings.EnableFileDownloads && !string.IsNullOrWhiteSpace(info.FilesPath))
        {
            contentActionsRow
                .WithButton("Files", $"files|{name}", ButtonStyle.Secondary);
            includeContentActionsRow = true;
        }
        if (AppSettings.EnableGallery && !string.IsNullOrWhiteSpace(info.GalleryPath))
        {
            contentActionsRow
                .WithButton("Gallery", $"gallery|{name}|1", ButtonStyle.Secondary);
            includeContentActionsRow = true;
        }

        if (includeContentActionsRow)
        {
            component.AddRow(contentActionsRow);
        }

        return component.Build();
    }

    [ComponentInteraction("status|*", true)]
    [SlashCommand("status", "Provides the status of a server.")]
    public async Task Status([Autocomplete(typeof(ServersAutocompleteHandler))] string name)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            var status = await ServerManager.GetServerStatusAsync(name);

            await FollowupAsync($"The status of the `{name}` server is `{status}`.", ephemeral: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in status interaction for server '{Name}'.", name);
            await FollowupAsync($"Interaction failed. See logs for details.", ephemeral: true);
        }
    }

    [ComponentInteraction("start|*", true)]
    [SlashCommand("start", "Starts a server.")]
    public async Task Start([Autocomplete(typeof(ServersAutocompleteHandler))] string name)
    {
        await RespondAsync($"The `{name}` server is starting...", ephemeral: true);
        try
        {
            await ServerManager.StartServerAsync(name, wait: true);

            await FollowupAsync($"{Context.User.GlobalName} started the `{name}` server.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting server '{Name}'.", name);
            await FollowupAsync($"Interaction failed. See logs for details.", ephemeral: true);
        }
    }

    [ComponentInteraction("restart|*", true)]
    [SlashCommand("restart", "Restarts a server.")]
    public async Task Restart([Autocomplete(typeof(ServersAutocompleteHandler))] string name)
    {
        await RespondAsync($"The `{name}` server is restarting...", ephemeral: true);

        try
        {
            await ServerManager.StopServerAsync(name, wait: true);
            await ServerManager.StartServerAsync(name, wait: true);

            await FollowupAsync($"{Context.User.GlobalName} restarted the `{name}` server.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error restarting server '{Name}'.", name);
            await FollowupAsync($"Interaction failed. See logs for details.", ephemeral: true);
        }
    }

    [ComponentInteraction("stop|*", true)]
    [SlashCommand("stop", "Stops a server.")]
    public async Task Stop([Autocomplete(typeof(ServersAutocompleteHandler))] string name)
    {
        await RespondAsync($"The `{name}` server is stopping...", ephemeral: true);
        try
        {
            await ServerManager.StopServerAsync(name, wait: true);

            await FollowupAsync($"{Context.User.GlobalName} stopped the `{name}` server.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping server '{Name}'.", name);
            await FollowupAsync($"Interaction failed. See logs for details.", ephemeral: true);
        }
    }

    [ComponentInteraction("logs|*", true)]
    [SlashCommand("logs", "Provides the logs of a server.")]
    public async Task Logs([Autocomplete(typeof(ServersAutocompleteHandler))] string name)
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
            Logger.LogError(ex, "Error in logs interaction for server '{Name}'.", name);
            await FollowupAsync($"Interaction failed. See logs for details.", ephemeral: true);
        }
    }

    [ComponentInteraction("readme|*", true)]
    [SlashCommand("readme", "Provides the readme of a server.")]
    public async Task Readme([Autocomplete(typeof(ServersAutocompleteHandler))] string name)
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
            Logger.LogError(ex, "Error in readme interaction for server '{Name}'.", name);
            await FollowupAsync($"Interaction failed. See logs for details.", ephemeral: true);
        }
    }

    [ComponentInteraction("files|*", true)]
    [SlashCommand("files", "Provides the files of a server.")]
    public async Task Files([Autocomplete(typeof(ServersAutocompleteHandler))] string name)
    {
        if (!AppSettings.EnableFileDownloads)
        {
            await FollowupAsync("File downloads are disabled.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        try
        {
            var files = (await ServerManager.GetServerFilesAsync(name)).ToArray();

            if (files.Length == 0)
            {
                await FollowupAsync("No files found.", ephemeral: true);
                return;
            }

            files = files.OrderByDescending(f => f.Name).ToArray();

            var selectMenu = new SelectMenuBuilder();
            selectMenu.CustomId = $"file|{name}";
            selectMenu.WithOptions(files.Select(f => new SelectMenuOptionBuilder()
            {
                Label = f.Name,
                Value = f.Name
            }).ToList());

            var component = new ComponentBuilder();
            var row = new ActionRowBuilder();
            component.WithRows([row]);
            row.WithSelectMenu(selectMenu);

            await FollowupAsync($"Select a `{name}` server file to download:", components: component.Build(), ephemeral: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in files interaction for server '{Name}'.", name);
            await FollowupAsync($"Interaction failed. See logs for details.", ephemeral: true);
        }
    }

    [ComponentInteraction("file|*", true)]
    public async Task DownloadFile(string name, string fileName)
    {
        if (!AppSettings.EnableFileDownloads)
        {
            await FollowupAsync("File downloads are disabled.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        try
        {
            var fileInfo = await ServerManager.GetServerFileAsync(name, fileName);

            if (fileInfo.Length < 25 * 1024 * 1024) // 25MB for Discord files
            {
                using (var stream = fileInfo.OpenRead())
                {
                    await FollowupWithFileAsync(stream, fileName, ephemeral: true);
                }
            }
            else if (AppSettings.LargeFileDownloadHandler != LargeFileDownloadHandlerType.Disabled)
            {
                var downloadUrl = await LargeFileDownloadHandler.GetDownloadUrlAsync(fileInfo);

                await FollowupAsync($"The file is too large to send directly. Download it from [here]({downloadUrl}).", ephemeral: true);
            }
            else
            {
                await FollowupAsync("The file is too large to send.", ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in file interaction for server '{Name}'.", name);
            await FollowupAsync($"Interaction failed. See logs for details.", ephemeral: true);
        }
    }

    [ComponentInteraction("gallery|*|*", true)]
    [SlashCommand("gallery", "Provides the gallery of a server.")]
    public async Task Gallery([Autocomplete(typeof(ServersAutocompleteHandler))] string name, int startIndex = 1)
    {
        await DeferAsync(ephemeral: true);

        if (startIndex < 1)
        {
            await FollowupAsync("Start index must be greater than 0.", ephemeral: true);
            return;
        }

        if (!AppSettings.EnableGallery)
        {
            await FollowupAsync("Gallery is disabled.", ephemeral: true);
            return;
        }

        try
        {
            var files = (await ServerManager.GetServerGalleryFilesAsync(name)).ToArray();

            if (files.Length == 0)
            {
                await FollowupAsync("No gallery files found.", ephemeral: true);
                return;
            }

            var totalCount = files.Length;
            const int maxCount = 10; // Discord max
            const int maxSize =  25 * 1024 * 1024; // Discord max
            
            long fileSetSize = 0; 
            var fileSet = files
                .OrderByDescending(f => f.Name)
                .Skip(startIndex - 1)
                .Take(maxCount)
                .TakeWhile(f => (fileSetSize += f.Length) < maxSize)
                .ToArray();

            var component = new ComponentBuilder();
            var actionsRow = new ActionRowBuilder();
            component.AddRow(actionsRow);

            if (startIndex + fileSet.Length <= totalCount)
            {
                actionsRow.WithButton("Load more", $"gallery|{name}|{startIndex + fileSet.Length}", ButtonStyle.Primary);
            }

            if (AppSettings.EnableGalleryUploads)
            {
                actionsRow.WithButton("Upload", $"gallery-upload|{name}", ButtonStyle.Secondary);
            }

            var imageAttachments = fileSet.Select(f => new FileAttachment(f.OpenRead(), f.Name)).ToArray();

            await FollowupWithFilesAsync(
                text: $"Gallery files for the `{name}` server ({startIndex}-{startIndex + fileSet.Length - 1}/{totalCount}):",
                attachments: imageAttachments,
                components: component.Build(),
                ephemeral: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in gallery interaction for server '{Name}'.", name);
            await FollowupAsync($"Interaction failed. See logs for details.", ephemeral: true);
        }
    }

    [ComponentInteraction("gallery-upload|*", true)]
    public async Task GalleryUpload(string name)
    {
        if (!AppSettings.EnableGalleryUploads)
        {
            await FollowupAsync("Gallery uploads are disabled.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        try
        {
            var command = CommandManager.GetCommand("servers-gallup");
            await FollowupAsync($"Upload a file to the gallery using the </servers-gallup:{command.Id}> command.", ephemeral: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in gallery upload interaction for server '{Name}'.", name);
            await FollowupAsync($"Interaction failed. See logs for details.", ephemeral: true);
        }
    }

    [SlashCommand("gallery-upload", "Upload a file to a server gallery.")]
    public async Task GalleryUpload([Autocomplete(typeof(ServersAutocompleteHandler))] string name, IAttachment file)
    {
        if (!AppSettings.EnableGalleryUploads)
        {
            await FollowupAsync("Gallery uploads are disabled.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        try
        {
            await ServerManager.UploadServerGalleryFileAsync(name, file.Url, file.Filename);

            await FollowupAsync($"Uploaded file to the `{name}` server gallery.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in gallery upload command for server '{Name}'.", name);
            await FollowupAsync($"Interaction failed. See logs for details.", ephemeral: true);
        }
    }
}