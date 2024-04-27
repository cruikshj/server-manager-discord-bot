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

    public async Task<ServerStatus?> GetServerStatusAsync(string name, CancellationToken cancellationToken = default)
    {
        var server = await GetServerInfoAsync(name, cancellationToken);

        if (TryGetServerHost(server, out var adapter, out var identifier))
        {
            return await adapter.GetServerStatusAsync(identifier, cancellationToken);
        }

        return null;
    }

    public async Task StartServerAsync(string name, bool wait = false, CancellationToken cancellationToken = default)
    {
        var server = await GetServerInfoAsync(name, cancellationToken);

        if (TryGetServerHost(server, out var adapter, out var identifier))
        {
            await adapter.StartServerAsync(identifier, cancellationToken);
            
            if (wait)
            {
                if (!await adapter.WaitForServerStatusAsync(
                    identifier,
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

        if (TryGetServerHost(server, out var adapter, out var identifier))
        {
            await adapter.StopServerAsync(identifier, cancellationToken);

            if (wait)
            {
                if (!await adapter.WaitForServerStatusAsync(
                    identifier,
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

    public async Task<IDictionary<string, Stream>> GetServerLogsAsync(string name, CancellationToken cancellationToken = default)
    {
        var server = await GetServerInfoAsync(name, cancellationToken);

        if (TryGetServerHost(server, out var adapter, out var identifier))
        {
            return await adapter.GetServerLogsAsync(identifier);
        }
        else
        {
            throw new InvalidOperationException($"The `{name}` server does not support this operation.");
        }
    }

    private bool TryGetServerHost(
        ServerInfo server, 
        [NotNullWhen(true)]out IServerHostAdapter? serverHostAdapter, 
        [NotNullWhen(true)]out string? serverHostIdentifier)
    {
        if (!server.HasServerHost)
        {
            serverHostAdapter = null;
            serverHostIdentifier = null;
            return false;
        }

        serverHostAdapter = ServiceProvider.GetKeyedService<IServerHostAdapter>(server.ServerHostAdapter);
        serverHostIdentifier = server.ServerHostIdentifier!;

        if (serverHostAdapter == null)
        {
            return false;
        }

        return true;
    }
}