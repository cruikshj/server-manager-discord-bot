public class KubernetesConfigMapServerInfoProviderOptions
{
    public required string LabelSelector { get; set; } = "server-manager=default";

    public string? KubeConfigPath { get; set; }
}