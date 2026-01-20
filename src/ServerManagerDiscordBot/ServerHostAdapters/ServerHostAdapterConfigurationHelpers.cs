using k8s;
using Microsoft.Extensions.Options;

public static class ServerHostAdapterConfigurationHelpers
{
    public static readonly string ConfigurationKey = "ServerHostAdapters";

    public static WebApplicationBuilder ConfigureServerHostAdapters(this WebApplicationBuilder builder)
    {
        // Register shared infrastructure services
        builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();

        var children = builder.Configuration.GetSection(ConfigurationKey).GetChildren().ToList();
        foreach (var child in children)
        {
            var key = child.Key;

            var adapterType = child.GetValue<ServerHosterAdapterType>("Type");
            switch (adapterType)
            {
                case ServerHosterAdapterType.Process:
                    builder.Services.Configure<ProcessServerHostAdapterOptions>(key, child);
                    builder.Services.AddKeyedTransient<IServerHostAdapter, ProcessServerHostAdapter>(key, (sp, sk) =>
                    {
                        var options = sp.GetRequiredService<IOptionsSnapshot<ProcessServerHostAdapterOptions>>().Get(key);
                        var processRunner = sp.GetRequiredService<IProcessRunner>();
                        return new ProcessServerHostAdapter(options, processRunner);
                    });
                    break;
                case ServerHosterAdapterType.DockerCompose:
                    builder.Services.Configure<DockerComposeServerHostAdapterOptions>(key, child);
                    builder.Services.AddKeyedTransient<IServerHostAdapter, DockerComposeServerHostAdapter>(key, (sp, sk) =>
                    {
                        var options = sp.GetRequiredService<IOptionsSnapshot<DockerComposeServerHostAdapterOptions>>().Get(key);
                        var processRunner = sp.GetRequiredService<IProcessRunner>();
                        return new DockerComposeServerHostAdapter(options, processRunner);
                    });
                    break;
                case ServerHosterAdapterType.Kubernetes:
                    builder.Services.Configure<KubernetesServerHostAdapterOptions>(key, child);
                    builder.Services.AddKeyedTransient<IServerHostAdapter, KubernetesServerHostAdapter>(key, (sp, sk) =>
                    {
                        var options = sp.GetRequiredService<IOptionsSnapshot<KubernetesServerHostAdapterOptions>>().Get(key);
                        var kubeConfig = !string.IsNullOrEmpty(options.KubeConfigPath)
                            ? KubernetesClientConfiguration.BuildConfigFromConfigFile(options.KubeConfigPath)
                            : KubernetesClientConfiguration.InClusterConfig();
                        var kubernetesClientFactory = new KubernetesClientFactory(kubeConfig);
                        return new KubernetesServerHostAdapter(kubernetesClientFactory);
                    });
                    break;
            }
        }
        
        return builder;
    }
}
