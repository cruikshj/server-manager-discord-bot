using System.Diagnostics;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;

public class KubernetesClient(IOptions<AppSettings> appSettings)
{
    public AppSettings AppSettings { get; } = appSettings.Value;

    public KubernetesClientConfiguration KubeConfig { get; } = 
        !string.IsNullOrEmpty(appSettings.Value.KubeConfigPath) ? 
        KubernetesClientConfiguration.BuildConfigFromConfigFile(appSettings.Value.KubeConfigPath) :
        KubernetesClientConfiguration.InClusterConfig();

    public async Task<IDictionary<string, string>> GetServerConfigMapDataAsync()
    {
        using var client = new Kubernetes(KubeConfig);
        var configMaps = await client.ListConfigMapForAllNamespacesAsync(labelSelector: AppSettings.ServerConfigMapLabelSelector);

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

    public async Task<ServerStatus> GetDeploymentStatusAsync(string deploymentName)
    {
        var metadata = ParseDeploymentName(deploymentName);

        using var client = new Kubernetes(KubeConfig);
        var deployment = await client.ReadNamespacedDeploymentStatusAsync(metadata.Name, metadata.Namespace);
        
        return GetStatus(deployment);
    }

    public async Task<bool> WaitForDeploymentStatusAsync(string deploymentName, ServerStatus status, TimeSpan timeout)
    {
        var metadata = ParseDeploymentName(deploymentName);

        using var client = new Kubernetes(KubeConfig);
        
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            var deployment = await client.ReadNamespacedDeploymentStatusAsync(metadata.Name, metadata.Namespace);
            if (GetStatus(deployment) == status)
            {
                return true;
            }
            
            await Task.Delay(1000);
        }
        return false;
    }

    public async Task StartDeploymentAsync(string deploymentName)
    {        
        var metadata = ParseDeploymentName(deploymentName);

        using var client = new Kubernetes(KubeConfig);
        await client.PatchNamespacedDeploymentScaleAsync(
            new V1Patch(@"{ ""spec"": { ""replicas"": 1 } }", V1Patch.PatchType.MergePatch),
            metadata.Name, 
            metadata.Namespace);
    }

    public async Task StopDeploymentAsync(string deploymentName)
    {
        var metadata = ParseDeploymentName(deploymentName);
        
        using var client = new Kubernetes(KubeConfig);
        await client.PatchNamespacedDeploymentScaleAsync(
            new V1Patch(@"{ ""spec"": { ""replicas"": 0 } }", V1Patch.PatchType.MergePatch),
            metadata.Name, 
            metadata.Namespace);
    }

    public async Task<IDictionary<string, Stream>> GetDeploymentLogsAsync(string deploymentName)
    {        
        var metadata = ParseDeploymentName(deploymentName);
        
        using var client = new Kubernetes(KubeConfig);
        
        var deployment = await client.ReadNamespacedDeploymentStatusAsync(metadata.Name, metadata.Namespace);
        var matchLabels = deployment.Spec.Selector.MatchLabels;

        var pods = await client.ListNamespacedPodAsync(metadata.Namespace, labelSelector: string.Join(",", matchLabels.Select(x => $"{x.Key}={x.Value}")));
        var pod = pods.Items.FirstOrDefault(); 

        if (pod == null)
        {
            return new Dictionary<string, Stream>();
        }

        var containers = pod.Spec.Containers.Select(x => x.Name).ToList();

        var logs = new Dictionary<string, Stream>();
        foreach (var container in containers)
        {
            var logStream = await client.ReadNamespacedPodLogAsync(pod.Metadata.Name, metadata.Namespace, container: container);
            logs.Add(container, logStream);
        }

        return logs;
    }

    private static ServerStatus GetStatus(V1Deployment deployment)
    {
        var status = deployment.Status;

        var replicas = status.Replicas.GetValueOrDefault(0);

        if (replicas != 0 && status.ReadyReplicas == replicas)
        {
            return ServerStatus.Running;
        }

        if (replicas != 0)
        {
            return ServerStatus.Starting;
        }

        return ServerStatus.Stopped;
    }

    private static (string Name, string Namespace) ParseDeploymentName(string deploymentName)
    {
        var parts = deploymentName.Split('/');
        if (parts.Length == 1)
        {
            return (parts[0], parts[0]);
        }
        return (parts[1], parts[0]);
    }
}