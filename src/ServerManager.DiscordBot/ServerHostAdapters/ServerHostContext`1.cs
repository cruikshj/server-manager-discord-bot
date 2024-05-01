using Microsoft.Extensions.Options;

public class ServerHostContext<TProperties> : ServerHostContext
    where TProperties : class, new()
{
    public ServerHostContext(ServerHostContext context)
        : base(context.ServerName, context.AdapterName, context.Properties)
    {
        if (context.Properties is not null)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(context.Properties)
                .Build();
            var services = new ServiceCollection();
            services.AddOptions<TProperties>()
                .Bind(configuration)
                .ValidateDataAnnotations();
            var serviceProvider = services.BuildServiceProvider();
            Properties = serviceProvider.GetRequiredService<IOptions<TProperties>>().Value;
        }
        else
        {
            Properties = new TProperties();
        }
    }

    public new TProperties Properties { get; init; }
}