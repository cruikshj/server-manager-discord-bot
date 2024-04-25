using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class ServerManager(
    IOptions<AppSettings> appSettings,
    KubernetesClient kubernetesClient,
    IMemoryCache memoryCache)
{
    public AppSettings AppSettings { get; } = appSettings.Value;
    public KubernetesClient KubernetesClient { get; } = kubernetesClient;
    public IMemoryCache MemoryCache { get; } = memoryCache;
    private static object ServersKey = new object();

    public async Task<IDictionary<string, ServerInfo>> GetServersAsync()
    {
        if (MemoryCache.TryGetValue(ServersKey, out Dictionary<string, ServerInfo>? servers) && servers != null)
        {
            return servers;
        }

        var configMapData = await KubernetesClient.GetServerConfigMapDataAsync();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        servers = new Dictionary<string, ServerInfo>();

        foreach (var (name, data) in configMapData)
        {
            var server = deserializer.Deserialize<ServerInfo>(data);
            servers.Add(name, server);
        }

        MemoryCache.Set(ServersKey, servers, AppSettings.ServersCacheExpiration);

        return servers;
    }

    public async Task<ServerInfo> GetServerInfoAsync(string name)
    {
        var servers = await GetServersAsync();

        if (!servers.TryGetValue(name, out var server))
        {
            throw new ArgumentException($"Server '{name}' not found.", nameof(name));
        }

        return server;
    }

    public async Task<ServerStatus?> GetServerStatusAsync(string name)
    {
        var server = await GetServerInfoAsync(name);

        if (!string.IsNullOrWhiteSpace(server.Deployment))
        {
            return await KubernetesClient.GetDeploymentStatusAsync(server.Deployment);
        }

        return null;
    }

    public async Task StartServerAsync(string name, bool wait = false)
    {
        var server = await GetServerInfoAsync(name);

        if (!string.IsNullOrWhiteSpace(server.Deployment))
        {
            await KubernetesClient.StartDeploymentAsync(
                server.Deployment);
            
            if (wait)
            {
                if (!await KubernetesClient.WaitForDeploymentStatusAsync(
                    server.Deployment,
                    ServerStatus.Running,
                    TimeSpan.FromMinutes(10)))
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

    public async Task StopServerAsync(string name, bool wait = false)
    {
        var server = await GetServerInfoAsync(name);

        if (!string.IsNullOrWhiteSpace(server.Deployment))
        {
            await KubernetesClient.StopDeploymentAsync(
                server.Deployment);

            if (wait)
            {
                if (!await KubernetesClient.WaitForDeploymentStatusAsync(
                    server.Deployment,
                    ServerStatus.Stopped,
                    TimeSpan.FromMinutes(10)))
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

    public async Task<IEnumerable<FileInfo>> GetServerFilesAsync(string name)
    {
        var server = await GetServerInfoAsync(name);

        if (server.FilesPath == null)
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

    public async Task<FileInfo> GetServerFileAsync(string name, string fileName)
    {
        var server = await GetServerInfoAsync(name);

        if (server.FilesPath == null)
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

    public async Task<IDictionary<string, Stream>> GetServerLogsAsync(string name)
    {
        var server = await GetServerInfoAsync(name);

        if (!string.IsNullOrWhiteSpace(server.Deployment))
        {
            return await KubernetesClient.GetDeploymentLogsAsync(server.Deployment);
        }
        else
        {
            throw new InvalidOperationException($"The `{name}` server does not support this operation.");
        }
    }
}