using k8s;

/// <summary>
/// Factory for creating Kubernetes clients, enabling testability.
/// </summary>
public interface IKubernetesClientFactory
{
    /// <summary>
    /// Creates a new Kubernetes client.
    /// </summary>
    IKubernetes CreateClient();
}

/// <summary>
/// Default implementation of IKubernetesClientFactory.
/// </summary>
public class KubernetesClientFactory : IKubernetesClientFactory
{
    private readonly KubernetesClientConfiguration _config;

    public KubernetesClientFactory(KubernetesClientConfiguration config)
    {
        _config = config;
    }

    public IKubernetes CreateClient()
    {
        return new Kubernetes(_config);
    }
}
