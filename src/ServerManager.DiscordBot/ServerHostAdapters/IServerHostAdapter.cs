public interface IServerHostAdapter
{
    ServerHostContext Context { get; set; }

    Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken = default);

    Task<bool> WaitForServerStatusAsync(ServerStatus status, TimeSpan timeout, CancellationToken cancellationToken = default);

    Task StartServerAsync(CancellationToken cancellationToken = default);

    Task StopServerAsync(CancellationToken cancellationToken = default);

    Task<IDictionary<string, Stream>> GetServerLogsAsync(CancellationToken cancellationToken = default);
}