public class AppSettings 
{
    public required string BotToken { get; set; }
    
    public required Uri HostUri { get; set; }

    public required List<ulong> GuildIds { get; set; } = [];

    public TimeSpan ServersCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);

    public string? KubeConfigPath { get; set; }

    public required string ServerConfigMapLabelSelector { get; set; } = "server-manager=default";
    
    public bool EnableLargeFileDownloads { get; set; } = false;

    public TimeSpan DownloadLinkExpiration { get; set; } = TimeSpan.FromDays(1);
}
