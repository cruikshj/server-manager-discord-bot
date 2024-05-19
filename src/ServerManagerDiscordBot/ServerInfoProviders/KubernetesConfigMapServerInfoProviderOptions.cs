using Microsoft.Extensions.Options;

public class KubernetesConfigMapServerInfoProviderOptions : IOptions<KubernetesConfigMapServerInfoProviderOptions>
{
    public required string LabelSelector { get; set; } = "server-manager=default";

    public string? KubeConfigPath { get; set; }

    KubernetesConfigMapServerInfoProviderOptions IOptions<KubernetesConfigMapServerInfoProviderOptions>.Value => this;
}