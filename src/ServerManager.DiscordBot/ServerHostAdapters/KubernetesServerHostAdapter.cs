using System.Diagnostics;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;

public class KubernetesServerHostAdapter(
    IOptions<KubernetesServerHostAdapterOptions> options)
    : IServerHostAdapter
{
    public KubernetesClientConfiguration KubeConfig { get; } = 
        !string.IsNullOrEmpty(options.Value.KubeConfigPath) ? 
        KubernetesClientConfiguration.BuildConfigFromConfigFile(options.Value.KubeConfigPath) :
        KubernetesClientConfiguration.InClusterConfig();

    public async Task<ServerStatus> GetServerStatusAsync(string identifier, CancellationToken cancellationToken = default)
    {
        using var client = new Kubernetes(KubeConfig);

        return await GetServerStatusAsync(client, identifier, cancellationToken);
    }

    public async Task<bool> WaitForServerStatusAsync(string identifier, ServerStatus status, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var client = new Kubernetes(KubeConfig);
        
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout && !cancellationToken.IsCancellationRequested)
        {
            var currentStatus = await GetServerStatusAsync(client, identifier, cancellationToken);
            if (currentStatus == status)
            {
                return true;
            }
            
            await Task.Delay(1000, cancellationToken);
        }
        return false;
    }

    private async Task<ServerStatus> GetServerStatusAsync(Kubernetes client, string identifier, CancellationToken cancellationToken)
    {
        var metadata = ParseIdentifier(identifier);

        ServerStatus GetStatus(int? replicas, int? readyReplicas)
        {
            replicas ??= 0;
            readyReplicas ??= 0;

            if (replicas != 0 && readyReplicas == replicas)
            {
                return ServerStatus.Running;
            }

            if (replicas != 0)
            {
                return ServerStatus.Starting;
            }

            return ServerStatus.Stopped;
        }

        switch (metadata.Kind)
        {
            case "Deployment":
                var deployment = await client.ReadNamespacedDeploymentStatusAsync(metadata.Name, metadata.Namespace, cancellationToken: cancellationToken);
                return GetStatus(deployment.Status.Replicas, deployment.Status.ReadyReplicas);
            case "StatefulSet":
                var statefulSet = await client.ReadNamespacedStatefulSetStatusAsync(metadata.Name, metadata.Namespace, cancellationToken: cancellationToken);
                return GetStatus(statefulSet.Status.Replicas, statefulSet.Status.ReadyReplicas);
            default:
                throw new NotSupportedException($"Kind {metadata.Kind} is not supported.");
        }
    }

    public Task StartServerAsync(string identifier, CancellationToken cancellationToken = default)
    {
        return SetServerReplicasAsync(identifier, 1, cancellationToken);
    }

    public Task StopServerAsync(string identifier, CancellationToken cancellationToken = default)
    {
        return SetServerReplicasAsync(identifier, 0, cancellationToken);
    }

    private Task SetServerReplicasAsync(string identifier, int replicas, CancellationToken cancellationToken)
    {
        var metadata = ParseIdentifier(identifier);
        
        using var client = new Kubernetes(KubeConfig);

        var patch = new V1Patch($@"{{ ""spec"": {{ ""replicas"": {replicas} }} }}", V1Patch.PatchType.MergePatch);

        return metadata.Kind switch
        {
            "Deployment" => client.PatchNamespacedDeploymentScaleAsync(
                patch,
                metadata.Name, 
                metadata.Namespace,
                cancellationToken: cancellationToken),
            "StatefulSet" => client.PatchNamespacedStatefulSetScaleAsync(
                patch,
                metadata.Name, 
                metadata.Namespace,
                cancellationToken: cancellationToken),
            _ => throw new NotSupportedException($"Kind {metadata.Kind} is not supported.")
        };
    }

    public async Task<IDictionary<string, Stream>> GetServerLogsAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var metadata = ParseIdentifier(identifier);
        
        using var client = new Kubernetes(KubeConfig);

        switch (metadata.Kind)
        {
            case "Deployment":
                var deployment = await client.ReadNamespacedDeploymentStatusAsync(metadata.Name, metadata.Namespace, cancellationToken: cancellationToken);
                return await GetPodLogsAsync(client, deployment.Spec.Selector.MatchLabels, metadata.Namespace, cancellationToken);
            case "StatefulSet":
                var statefulSet = await client.ReadNamespacedStatefulSetStatusAsync(metadata.Name, metadata.Namespace, cancellationToken: cancellationToken);
                return await GetPodLogsAsync(client, statefulSet.Spec.Selector.MatchLabels, metadata.Namespace, cancellationToken);
            default:
                throw new NotSupportedException($"Kind {metadata.Kind} is not supported.");
        }
    }

    private async Task<IDictionary<string, Stream>> GetPodLogsAsync(Kubernetes client, IDictionary<string, string> matchLabels, string ns, CancellationToken cancellationToken)
    {
        var labelSelector = string.Join(",", matchLabels.Select(x => $"{x.Key}={x.Value}"));
        var pods = await client.ListNamespacedPodAsync(ns, labelSelector: labelSelector, cancellationToken: cancellationToken);
        var pod = pods.Items.FirstOrDefault();

        if (pod == null)
        {
            return new Dictionary<string, Stream>();
        }

        var containers = pod.Spec.Containers.Select(x => x.Name).ToList();

        var logs = new Dictionary<string, Stream>();
        foreach (var container in containers)
        {
            var logStream = await client.ReadNamespacedPodLogAsync(pod.Metadata.Name, ns, container: container, cancellationToken: cancellationToken);
            logs.Add(container, logStream);
        }

        return logs;
    }

    private static (string Kind, string Name, string Namespace) ParseIdentifier(string identifier)
    {
        try
        {
            var parts = identifier.Split(':');
            var kind = parts[0];
            parts = parts[1].Split('/');
            var name = parts[1];
            var ns = parts[0];
            return (kind, name, ns);
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                nameof(ServerInfo.ServerHostIdentifier), 
                "Invalid server host identifier. Use the format \"<Kind>:<Namespace>/<Name>\".", 
                ex);
        }
    }
}