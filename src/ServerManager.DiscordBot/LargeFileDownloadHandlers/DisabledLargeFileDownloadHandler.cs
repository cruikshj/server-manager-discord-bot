
public class DisabledLargeFileDownloadHandler : ILargeFileDownloadHandler
{
    public Task<Uri> GetDownloadUrlAsync(FileInfo fileInfo, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Large file downloads are disabled.");
    }
}