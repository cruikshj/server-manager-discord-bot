using Microsoft.Extensions.Options;

public static class ServerHostAdapterConfigurationHelpers
{
    public static readonly string ConfigurationKey = "ServerHostAdapters";

    public static WebApplicationBuilder ConfigureServerHostAdapters(this WebApplicationBuilder builder)
    {
        var children = builder.Configuration.GetSection(ConfigurationKey).GetChildren().ToList();
        foreach (var child in children)
        {
            var key = child.Key;

            var adapterType = child.GetValue<ServerHosterAdapterType>("Type");
            switch (adapterType)
            {
                case ServerHosterAdapterType.Executable:
                    builder.Services.Configure<ExecutableServerHostAdapterOptions>(key, child);
                    builder.Services.AddKeyedTransient<IServerHostAdapter, ExecutableServerHostAdapter>(key, (sp, sk) =>
                    {
                        var options = sp.GetRequiredService<IOptionsSnapshot<ExecutableServerHostAdapterOptions>>().Get(key);
                        return new ExecutableServerHostAdapter(options);
                    });
                    break;
                case ServerHosterAdapterType.Kubernetes:
                    builder.Services.Configure<KubernetesServerHostAdapterOptions>(key, child);
                    builder.Services.AddKeyedTransient<IServerHostAdapter, KubernetesServerHostAdapter>(key, (sp, sk) =>
                    {
                        var options = sp.GetRequiredService<IOptionsSnapshot<KubernetesServerHostAdapterOptions>>().Get(key);
                        return new KubernetesServerHostAdapter(options);
                    });
                    break;
            }
        }
        
        return builder;
    }
}