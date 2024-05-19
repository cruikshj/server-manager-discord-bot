public interface ILargeFileDownloadHandler
{
    Task<Uri> GetDownloadUrlAsync(FileInfo fileInfo, CancellationToken cancellationToken = default);

    void MapEndpoints(WebApplication app) {}
}