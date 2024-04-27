public interface IServerHostAdapter
{
    Task<ServerStatus> GetServerStatusAsync(string identifier, CancellationToken cancellationToken = default);

    Task<bool> WaitForServerStatusAsync(string identifier, ServerStatus status, TimeSpan timeout, CancellationToken cancellationToken = default);

    Task StartServerAsync(string identifier, CancellationToken cancellationToken = default);

    Task StopServerAsync(string identifier, CancellationToken cancellationToken = default);

    Task<IDictionary<string, Stream>> GetServerLogsAsync(string identifier, CancellationToken cancellationToken = default);
}