public static class ServerInfoProviderConfigurationHelpers
{
    public static readonly string ServerInfoProvidersKey = "ServerInfoProviders";

    public static WebApplicationBuilder ConfigureServerInfoProviders(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IServerInfoProvider, ConfigurationServerInfoProvider>();

        var children = builder.Configuration.GetSection(ServerInfoProvidersKey).GetChildren().ToList();
        foreach (var child in children)
        {
            var providerType = child.GetValue<ServerInfoProviderType>("Type");
            switch (providerType)
            {
                case ServerInfoProviderType.KubernetesConfigMap:
                    builder.Services.AddTransient<IServerInfoProvider, KubernetesConfigMapServerInfoProvider>();
                    builder.Services.Configure<KubernetesConfigMapServerInfoProviderOptions>(child);
                    break;
            }
        }
        
        return builder;
    }
}