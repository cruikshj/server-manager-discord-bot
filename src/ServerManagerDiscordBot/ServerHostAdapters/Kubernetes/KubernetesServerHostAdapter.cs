using System.Diagnostics;
using k8s;
using k8s.Models;

public class KubernetesServerHostAdapter(
    IKubernetesClientFactory kubernetesClientFactory)
    : ServerHostAdapterBase<KubernetesServerHostProperties>
{
    public IKubernetesClientFactory KubernetesClientFactory { get; } = kubernetesClientFactory;

    public override async Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        using var client = KubernetesClientFactory.CreateClient();

        return await GetServerStatusAsync(client, cancellationToken);
    }

    public override async Task<bool> WaitForServerStatusAsync(ServerStatus status, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var client = KubernetesClientFactory.CreateClient();
        
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout && !cancellationToken.IsCancellationRequested)
        {
            var currentStatus = await GetServerStatusAsync(client, cancellationToken);
            if (currentStatus == status)
            {
                return true;
            }
            
            await Task.Delay(1000, cancellationToken);
        }
        return false;
    }

    private async Task<ServerStatus> GetServerStatusAsync(IKubernetes client, CancellationToken cancellationToken)
    {
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

        switch (Context.Properties.Kind)
        {
            case "Deployment":
                var deployment = await client.AppsV1.ReadNamespacedDeploymentStatusAsync(Context.Properties.Name, Context.Properties.Namespace, cancellationToken: cancellationToken);
                return GetStatus(deployment.Status.Replicas, deployment.Status.ReadyReplicas);
            case "StatefulSet":
                var statefulSet = await client.AppsV1.ReadNamespacedStatefulSetStatusAsync(Context.Properties.Name, Context.Properties.Namespace, cancellationToken: cancellationToken);
                return GetStatus(statefulSet.Status.Replicas, statefulSet.Status.ReadyReplicas);
            default:
                throw new NotSupportedException($"Kind {Context.Properties.Kind} is not supported.");
        }
    }

    public override async Task StartServerAsync(CancellationToken cancellationToken = default)
    {
        await SetServerReplicasAsync(1, cancellationToken);
    }

    public override async Task StopServerAsync(CancellationToken cancellationToken = default)
    {
        await SetServerReplicasAsync(0, cancellationToken);
    }

    private async Task SetServerReplicasAsync(int replicas, CancellationToken cancellationToken)
    {        
        using var client = KubernetesClientFactory.CreateClient();

        var patch = new V1Patch($@"{{ ""spec"": {{ ""replicas"": {replicas} }} }}", V1Patch.PatchType.MergePatch);

        switch (Context.Properties.Kind)
        {
            case "Deployment":
                await client.AppsV1.PatchNamespacedDeploymentScaleAsync(
                    patch,
                    Context.Properties.Name, 
                    Context.Properties.Namespace,
                    cancellationToken: cancellationToken);
                break;
            case "StatefulSet":
                await client.AppsV1.PatchNamespacedStatefulSetScaleAsync(
                    patch,
                    Context.Properties.Name, 
                    Context.Properties.Namespace,
                    cancellationToken: cancellationToken);
                break;
            default:
                throw new NotSupportedException($"Kind {Context.Properties.Kind} is not supported.");
        }
    }

    public override async Task<IDictionary<string, Stream>> GetServerLogsAsync(CancellationToken cancellationToken = default)
    {        
        using var client = KubernetesClientFactory.CreateClient();

        switch (Context.Properties.Kind)
        {
            case "Deployment":
                var deployment = await client.AppsV1.ReadNamespacedDeploymentStatusAsync(Context.Properties.Name, Context.Properties.Namespace, cancellationToken: cancellationToken);
                return await GetPodLogsAsync(client, deployment.Spec.Selector.MatchLabels, Context.Properties.Namespace, cancellationToken);
            case "StatefulSet":
                var statefulSet = await client.AppsV1.ReadNamespacedStatefulSetStatusAsync(Context.Properties.Name, Context.Properties.Namespace, cancellationToken: cancellationToken);
                return await GetPodLogsAsync(client, statefulSet.Spec.Selector.MatchLabels, Context.Properties.Namespace, cancellationToken);
            default:
                throw new NotSupportedException($"Kind {Context.Properties.Kind} is not supported.");
        }
    }

    private static async Task<IDictionary<string, Stream>> GetPodLogsAsync(IKubernetes client, IDictionary<string, string> matchLabels, string ns, CancellationToken cancellationToken)
    {
        var labelSelector = string.Join(",", matchLabels.Select(x => $"{x.Key}={x.Value}"));
        var pods = await client.CoreV1.ListNamespacedPodAsync(ns, labelSelector: labelSelector, cancellationToken: cancellationToken);
        var pod = pods.Items.FirstOrDefault();

        if (pod == null)
        {
            return new Dictionary<string, Stream>();
        }

        var containers = pod.Spec.Containers.Select(x => x.Name).ToList();

        var logs = new Dictionary<string, Stream>();
        foreach (var container in containers)
        {
            var logStream = await client.CoreV1.ReadNamespacedPodLogAsync(pod.Metadata.Name, ns, container: container, cancellationToken: cancellationToken);
            logs.Add(container, logStream);
        }

        return logs;
    }
}
