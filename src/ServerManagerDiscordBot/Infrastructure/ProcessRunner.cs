using System.Diagnostics;

/// <summary>
/// Abstraction for running external processes, enabling testability.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Starts a new process and returns a handle to it.
    /// </summary>
    IProcessHandle Start(ProcessStartInfo startInfo);

    /// <summary>
    /// Gets all processes with the specified name.
    /// </summary>
    IProcessHandle[] GetProcessesByName(string name);
}

/// <summary>
/// Abstraction for a running process, enabling testability.
/// </summary>
public interface IProcessHandle : IDisposable
{
    int Id { get; }
    int ExitCode { get; }
    bool HasExited { get; }
    string? MainModuleFileName { get; }
    StreamReader StandardOutput { get; }
    StreamReader StandardError { get; }

    void Start();
    void Kill();
    Task WaitForExitAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of IProcessRunner that uses System.Diagnostics.Process.
/// </summary>
public class ProcessRunner : IProcessRunner
{
    public IProcessHandle Start(ProcessStartInfo startInfo)
    {
        var process = new Process { StartInfo = startInfo };
        var handle = new ProcessHandle(process);
        handle.Start();
        return handle;
    }

    public IProcessHandle[] GetProcessesByName(string name)
    {
        return Process.GetProcessesByName(name)
            .Select(p => new ProcessHandle(p))
            .ToArray();
    }
}

/// <summary>
/// Default implementation of IProcessHandle wrapping System.Diagnostics.Process.
/// </summary>
public class ProcessHandle : IProcessHandle
{
    private readonly Process _process;

    public ProcessHandle(Process process)
    {
        _process = process;
    }

    public int Id => _process.Id;
    public int ExitCode => _process.ExitCode;
    public bool HasExited => _process.HasExited;
    public string? MainModuleFileName => _process.MainModule?.FileName;
    public StreamReader StandardOutput => _process.StandardOutput;
    public StreamReader StandardError => _process.StandardError;

    public void Start() => _process.Start();
    public void Kill() => _process.Kill();
    public Task WaitForExitAsync(CancellationToken cancellationToken = default) 
        => _process.WaitForExitAsync(cancellationToken);

    public void Dispose() => _process.Dispose();
}
