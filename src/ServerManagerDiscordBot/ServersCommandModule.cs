using System.Text;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Options;
using SmartFormat;

public class ServersCommandModule(
    IOptions<AppSettings> appSettings,
    ServerManager serverManager,
    ILargeFileDownloadHandler largeFileDownloadHandler,
    ILogger<ServersCommandModule> logger)
    : InteractionModuleBase
{
    public AppSettings AppSettings { get; } = appSettings.Value;
    public ServerManager ServerManager { get; } = serverManager;
    public ILargeFileDownloadHandler LargeFileDownloadHandler { get; } = largeFileDownloadHandler;
    public ILogger Logger { get; } = logger;

    [SlashCommand("servers", "Display server information.")]
    public async Task Servers([Autocomplete(typeof(ServersAutocompleteHandler))] string? name = null)
    {
        await DeferAsync(ephemeral: true);

        try
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
            await FollowupAsync($"Error: {ex.Message}", ephemeral: true);
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

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }

    private async Task Info(string name)
    {
        var info = await ServerManager.GetServerInfoAsync(name);
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

        var component = new ComponentBuilder();

        if (!string.IsNullOrWhiteSpace(info.HostAdapterName))
        {
            var hostActionsRow = new ActionRowBuilder();
            hostActionsRow
                .WithButton("Status", $"status|{name}", ButtonStyle.Secondary)
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

        await FollowupAsync(embed: embed.Build(), components: component.Build(), ephemeral: true);
    }

    [ComponentInteraction("status|*")]
    public async Task Status(string name)
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
            await FollowupAsync($"Error: {ex.Message}", ephemeral: true);
        }
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
            Logger.LogError(ex, "Error starting server '{Name}'.", name);
            await FollowupAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    [ComponentInteraction("restart|*")]
    public async Task Restart(string name)
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
            Logger.LogError(ex, "Error stopping server '{Name}'.", name);
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
            Logger.LogError(ex, "Error in logs interaction for server '{Name}'.", name);
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
            Logger.LogError(ex, "Error in readme interaction for server '{Name}'.", name);
            await FollowupAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    [ComponentInteraction("files|*")]
    public async Task Files(string name)
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
            await FollowupAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    [ComponentInteraction("file|*")]
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
            await FollowupAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    [ComponentInteraction("gallery|*|*")]
    public async Task Gallery(string name, int page)
    {
        if (!AppSettings.EnableGallery)
        {
            await FollowupAsync("Gallery is disabled.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        try
        {
            var files = (await ServerManager.GetServerGalleryFilesAsync(name)).ToArray();

            if (files.Length == 0)
            {
                await FollowupAsync("No gallery files found.", ephemeral: true);
                return;
            }

            const int pageSize = 10; // Discord max image count
            var hasPages = files.Length > pageSize;
            var hasMore = files.Length > pageSize * page;
            files = files.OrderByDescending(f => f.Name).Skip((page - 1) * pageSize).Take(pageSize).ToArray();

            var component = new ComponentBuilder();
            var actionsRow = new ActionRowBuilder();
            component.AddRow(actionsRow);

            if (hasMore)
            {
                actionsRow.WithButton("Load more", $"gallery|{name}|{page + 1}", ButtonStyle.Primary);
            }

            if (AppSettings.EnableGalleryUploads)
            {
                actionsRow.WithButton("Upload", $"galleryupload|{name}", ButtonStyle.Secondary);
            }

            var imageAttachments = files.Select(f => new FileAttachment(f.OpenRead(), f.Name)).ToArray();

            var pageText = hasPages ? $" (page {page})" : "";

            await FollowupWithFilesAsync(
                text: $"Gallery files for the `{name}` server{pageText}:",
                attachments: imageAttachments, 
                components: component.Build(), 
                ephemeral: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in gallery interaction for server '{Name}'.", name);
            await FollowupAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }
}