
public class ConfigurationServerInfoProvider(IConfiguration configuration) : IServerInfoProvider
{
    public IConfiguration Configuration { get; } = configuration;

    public static readonly string ConfigurationSectionName = "Servers";

    public Task<IDictionary<string, ServerInfo>> GetServerInfoAsync(CancellationToken cancellationToken = default)
    {
        var servers = new Dictionary<string, ServerInfo>();
        Configuration.GetSection(ConfigurationSectionName).Bind(servers);
        return Task.FromResult<IDictionary<string, ServerInfo>>(servers);
    }
}