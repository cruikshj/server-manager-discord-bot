public class AppSettings 
{
    public required string BotToken { get; set; }

    public List<ulong> GuildIds { get; set; } = [];
    
    public Uri? HostUri { get; set; }

    public bool EnableFileDownloads { get; set; } = true;
    
    public LargeFileDownloadHandlerType LargeFileDownloadHandler { get; set; } = LargeFileDownloadHandlerType.Disabled;

    public bool EnableGallery { get; set; } = true;

    public bool EnableGalleryUploads { get; set; } = false;

    public string[] GalleryFileExtensions { get; set; } = [ "png", "jpg", "jpeg", "gif", "webp" ];

    public TimeSpan ServersCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan DownloadLinkExpiration { get; set; } = TimeSpan.FromDays(1);

    public TimeSpan ServerStatusWaitTimeout { get; set; } = TimeSpan.FromMinutes(10);
}
