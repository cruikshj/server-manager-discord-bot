using Microsoft.Extensions.Options;

public class KubernetesServerHostAdapterOptions : IOptions<KubernetesServerHostAdapterOptions>
{
    public string? KubeConfigPath { get; set; }

    KubernetesServerHostAdapterOptions IOptions<KubernetesServerHostAdapterOptions>.Value => this;
}