public class AppSettings 
{
    public required string BotToken { get; set; }

    public required List<ulong> GuildIds { get; set; } = [];

    public required string ServerConfigMapLabelSelector { get; set; } = "server-manager=default";

    public string? KubeConfigPath { get; set; }
    
    public Uri? DownloadHostUri { get; set; }

    public TimeSpan ServersCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan DownloadCacheExpiration { get; set; } = TimeSpan.FromDays(1);
}
