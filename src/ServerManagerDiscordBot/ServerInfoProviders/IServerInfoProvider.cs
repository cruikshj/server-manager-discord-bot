public interface IServerInfoProvider
{
    Task<IDictionary<string, ServerInfo>> GetServerInfoAsync(CancellationToken cancellationToken = default);
}