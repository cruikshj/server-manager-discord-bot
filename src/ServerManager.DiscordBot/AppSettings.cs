public class AppSettings 
{
    public required string BotToken { get; set; }
    
    public required Uri HostUri { get; set; }

    public required List<ulong> GuildIds { get; set; } = [];

    public TimeSpan ServersCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);

    public bool EnableFileDownloads { get; set; } = true;
    
    public LargeFileDownloadHandlerType LargeFileDownloadHandler { get; set; } = LargeFileDownloadHandlerType.Disabled;

    public TimeSpan DownloadLinkExpiration { get; set; } = TimeSpan.FromDays(1);
}
