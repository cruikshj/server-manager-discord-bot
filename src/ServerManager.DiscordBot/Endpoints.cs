using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

public static class Endpoints
{
    public static void MapEndpoints(this WebApplication app)
    {
        var appSettings = app.Services.GetRequiredService<IOptions<AppSettings>>().Value;
        
        if (appSettings.DownloadHostUri is not null)
        {
            app.MapGet("/download/{key:guid}", (IMemoryCache memoryCache, IContentTypeProvider contentTypeProvider, HttpContext context, Guid key) =>
            {
                if (!memoryCache.TryGetValue<FileInfo>(key, out var fileInfo) ||
                    fileInfo is null ||
                    !fileInfo.Exists)
                {
                    return Results.NotFound();
                }

                if (!contentTypeProvider.TryGetContentType(fileInfo.Name, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                return Results.File(fileInfo.FullName, contentType, fileInfo.Name);
            });
        }
    }
}