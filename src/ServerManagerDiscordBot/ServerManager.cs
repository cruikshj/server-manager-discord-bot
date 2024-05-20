using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

public class ServerManager(
    IOptions<AppSettings> appSettings,
    IMemoryCache memoryCache,
    IEnumerable<IServerInfoProvider> serverInfoProviders,
    IServiceProvider serviceProvider)
{
    public AppSettings AppSettings { get; } = appSettings.Value;
    public IMemoryCache MemoryCache { get; } = memoryCache;
    public IEnumerable<IServerInfoProvider> ServerInfoProviders { get; } = serverInfoProviders;
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    private static object ServersKey = new object();

    public async Task<IDictionary<string, ServerInfo>> GetServersAsync(CancellationToken cancellationToken = default)
    {
        if (MemoryCache.TryGetValue(ServersKey, out Dictionary<string, ServerInfo>? servers) && servers != null)
        {
            return servers;
        }

        servers = new Dictionary<string, ServerInfo>();

        foreach (var provider in ServerInfoProviders)
        {
            var providerServers = await provider.GetServerInfoAsync(cancellationToken);
            foreach (var (name, server) in providerServers)
            {
                servers[name] = server;
            }
        }

        MemoryCache.Set(ServersKey, servers, AppSettings.ServersCacheExpiration);

        return servers;
    }

    public async Task<ServerInfo> GetServerInfoAsync(string name, CancellationToken cancellationToken = default)
    {
        var servers = await GetServersAsync(cancellationToken);

        if (!servers.TryGetValue(name, out var server))
        {
            throw new ArgumentException($"Server '{name}' not found.", nameof(name));
        }

        return server;
    }

    public async Task<ServerStatus> GetServerStatusAsync(string name, CancellationToken cancellationToken = default)
    {
        var server = await GetServerInfoAsync(name, cancellationToken);

        if (TryGetServerHostAdapter(name, server, out var adapter))
        {
            return await adapter.GetServerStatusAsync(cancellationToken);
        }

        return ServerStatus.Unknown;
    }

    public async Task StartServerAsync(string name, bool wait = false, CancellationToken cancellationToken = default)
    {
        var server = await GetServerInfoAsync(name, cancellationToken);

        if (TryGetServerHostAdapter(name, server, out var adapter))
        {
            await adapter.StartServerAsync(cancellationToken);
            
            if (wait)
            {
                if (!await adapter.WaitForServerStatusAsync(
                    ServerStatus.Running,
                    AppSettings.ServerStatusWaitTimeout,
                    cancellationToken))
                {
                    throw new TimeoutException($"The `{name}` server did not start within the timeout period.");
                }
            }
        }
        else
        {
            throw new InvalidOperationException($"The `{name}` server does not support this operation.");
        }
    }

    public async Task StopServerAsync(string name, bool wait = false, CancellationToken cancellationToken = default)
    {
        var server = await GetServerInfoAsync(name, cancellationToken);

        if (TryGetServerHostAdapter(name, server, out var adapter))
        {
            await adapter.StopServerAsync(cancellationToken);

            if (wait)
            {
                if (!await adapter.WaitForServerStatusAsync(
                    ServerStatus.Stopped,
                    AppSettings.ServerStatusWaitTimeout,
                    cancellationToken))
                {
                    throw new TimeoutException($"The `{name}` server did not stop within the timeout period.");
                }
            }
        }
        else
        {
            throw new InvalidOperationException($"The `{name}` server does not support this operation.");
        }
    }

    public async Task<IEnumerable<FileInfo>> GetServerFilesAsync(string name, CancellationToken cancellationToken = default)
    {
        var server = await GetServerInfoAsync(name, cancellationToken);

        if (string.IsNullOrWhiteSpace(server.FilesPath))
        {
            throw new InvalidOperationException($"The `{name}` server does not support this operation.");
        }

        var directory = new DirectoryInfo(server.FilesPath);
        if (!directory.Exists)
        {
            throw new DirectoryNotFoundException($"The `{name}` server files directory `{server.FilesPath}` does not exist.");
        }

        var files = directory.EnumerateFiles();

        return files;
    }

    public async Task<FileInfo> GetServerFileAsync(string name, string fileName, CancellationToken cancellationToken = default)
    {
        var server = await GetServerInfoAsync(name, cancellationToken);

        if (string.IsNullOrWhiteSpace(server.FilesPath))
        {
            throw new InvalidOperationException($"The `{name}` server does not support this operation.");
        }

        var filePath = Path.Combine(server.FilesPath, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The `{name}` server file `{fileName}` does not exist.");
        }

        return new FileInfo(filePath);
    }

    public async Task<IEnumerable<FileInfo>> GetServerGalleryFilesAsync(string name, CancellationToken cancellationToken = default)
    {
        var server = await GetServerInfoAsync(name, cancellationToken);

        if (string.IsNullOrWhiteSpace(server.GalleryPath))
        {
            throw new InvalidOperationException($"The `{name}` server does not support this operation.");
        }

        var directory = new DirectoryInfo(server.GalleryPath);
        if (!directory.Exists)
        {
            throw new DirectoryNotFoundException($"The `{name}` server gallery directory `{server.GalleryPath}` does not exist.");
        }

        var extensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
        var files = directory.EnumerateFiles()
                    .Where(file => extensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase));

        return files;
    }

    public async Task<IDictionary<string, Stream>> GetServerLogsAsync(string name, CancellationToken cancellationToken = default)
    {
        var server = await GetServerInfoAsync(name, cancellationToken);

        if (TryGetServerHostAdapter(name, server, out var adapter))
        {
            return await adapter.GetServerLogsAsync(cancellationToken);
        }
        else
        {
            throw new InvalidOperationException($"The `{name}` server does not support this operation.");
        }
    }

    private bool TryGetServerHostAdapter(
        string serverName,
        ServerInfo server, 
        [NotNullWhen(true)]out IServerHostAdapter? serverHostAdapter)
    {
        if (string.IsNullOrWhiteSpace(server.HostAdapterName))
        {
            serverHostAdapter = null;
            return false;
        }

        serverHostAdapter = ServiceProvider.GetKeyedService<IServerHostAdapter>(server.HostAdapterName);

        if (serverHostAdapter == null)
        {
            return false;
        }

        serverHostAdapter.Context = new ServerHostContext(serverName, server.HostAdapterName, server.HostProperties);

        return true;
    }
}