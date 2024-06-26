using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

public class BuiltInLargeFileDownloadHandler(
    IOptions<AppSettings> appSettings,
    IMemoryCache memoryCache,
    IServer server)
    : ILargeFileDownloadHandler
{
    public AppSettings AppSettings { get; } = appSettings.Value;
    public IMemoryCache MemoryCache { get; } = memoryCache;
    public IServer Server { get; } = server;

    public Task<Uri> GetDownloadUrlAsync(FileInfo fileInfo, CancellationToken cancellationToken = default)
    {
        var downloadKey = Guid.NewGuid();
        MemoryCache.Set(downloadKey, fileInfo, AppSettings.DownloadLinkExpiration);
        var hostUri = AppSettings.HostUri ?? new Uri(Server.GetDefaultServerAddress());
        var downloadUrl = new Uri(hostUri, $"/download/{downloadKey}");
        return Task.FromResult(downloadUrl);
    }

    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("/download/{key:guid}", (IMemoryCache memoryCache, HttpContext context, Guid key) =>
        {
            if (!memoryCache.TryGetValue<FileInfo>(key, out var fileInfo) ||
                fileInfo is null ||
                !fileInfo.Exists)
            {
                return Results.NotFound();
            }

            var contentTypeProvider = new FileExtensionContentTypeProvider();
            if (!contentTypeProvider.TryGetContentType(fileInfo.Name, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return Results.File(fileInfo.FullName, contentType, fileInfo.Name);
        });
    }
}