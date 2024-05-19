using k8s;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class KubernetesConfigMapServerInfoProvider(
    IOptions<KubernetesConfigMapServerInfoProviderOptions> options)
    : IServerInfoProvider
{
    public string LabelSelector { get; } = options.Value.LabelSelector;
    
    public KubernetesClientConfiguration KubeConfig { get; } = 
        !string.IsNullOrEmpty(options.Value.KubeConfigPath) ? 
        KubernetesClientConfiguration.BuildConfigFromConfigFile(options.Value.KubeConfigPath) :
        KubernetesClientConfiguration.InClusterConfig();

    public async Task<IDictionary<string, ServerInfo>> GetServerInfoAsync(CancellationToken cancellationToken = default)
    {
        var configMapData = await GetServerConfigMapDataAsync(cancellationToken);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var servers = new Dictionary<string, ServerInfo>();

        foreach (var (name, data) in configMapData)
        {
            var server = deserializer.Deserialize<ServerInfo>(data);
            servers.Add(name, server);
        }

        return servers;
    }

    private async Task<IDictionary<string, string>> GetServerConfigMapDataAsync(CancellationToken cancellationToken)
    {
        using var client = new Kubernetes(KubeConfig);
        
        var configMaps = await client.ListConfigMapForAllNamespacesAsync(
            labelSelector: LabelSelector,
            cancellationToken: cancellationToken);

        var result = new Dictionary<string, string>();

        foreach (var configMap in configMaps.Items)
        {
            foreach (var item in configMap.Data)
            {
                result.Add(item.Key, item.Value);
            }
        }

        return result;
    }
}