using System.Diagnostics;

public abstract class ServerHostAdapterBase<TProperties> : IServerHostAdapter
    where TProperties : class, new()
{   
    ServerHostContext IServerHostAdapter.Context
    {
        get => _context ?? throw new InvalidOperationException("Context is not set.");
        set => _context = new ServerHostContext<TProperties>(value);
    }

    public ServerHostContext<TProperties> Context
    {
        get => _context ?? throw new InvalidOperationException("Context is not set.");
        set => _context = value;
    }

    private ServerHostContext<TProperties>? _context;

    public abstract Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken);

    public virtual async Task<bool> WaitForServerStatusAsync(ServerStatus status, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout && !cancellationToken.IsCancellationRequested)
        {
            var currentStatus = await GetServerStatusAsync(cancellationToken);
            if (currentStatus == status)
            {
                return true;
            }
            
            await Task.Delay(1000, cancellationToken);
        }
        return false;
    }

    public abstract Task StartServerAsync(CancellationToken cancellationToken = default);

    public abstract Task StopServerAsync(CancellationToken cancellationToken = default);

    public abstract Task<IDictionary<string, Stream>> GetServerLogsAsync(CancellationToken cancellationToken = default);
}