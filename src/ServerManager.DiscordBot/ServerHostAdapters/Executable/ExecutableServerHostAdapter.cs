using System.Diagnostics;
using Microsoft.Extensions.Options;

public class ExecutableServerHostAdapter(
    IOptions<ExecutableServerHostAdapterOptions> options)
    : ServerHostAdapterBase<ExecutableServerHostProperties>
{
    public ExecutableServerHostAdapterOptions Options { get; } = options.Value;

    public override Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        var processName = Path.GetFileName(Context.Properties.FileName);

        var processes = Process.GetProcessesByName(processName);

        var status = processes.Length > 0
            ? ServerStatus.Running
            : ServerStatus.Stopped;

        return Task.FromResult(status);
    }

    public override async Task StartServerAsync(CancellationToken cancellationToken = default)
    {
        if (await GetServerStatusAsync(cancellationToken) == ServerStatus.Running)
        {
            throw new InvalidOperationException("Server is already running.");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Context.Properties.FileName,
                Arguments = Context.Properties.Arguments,
                WorkingDirectory = Context.Properties.WorkingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            }
        };

        process.Start();
    }

    public override async Task StopServerAsync(CancellationToken cancellationToken = default)
    {
        if (await GetServerStatusAsync(cancellationToken) == ServerStatus.Stopped)
        {
            throw new InvalidOperationException("Server is already stopped.");
        }

        var processName = Path.GetFileName(Context.Properties.FileName);

        var processes = Process.GetProcessesByName(processName);

        foreach (var process in processes)
        {
            process.Kill();
        }
    }

    public override Task<IDictionary<string, Stream>> GetServerLogsAsync(CancellationToken cancellationToken = default)
    {
        var processName = Path.GetFileName(Context.Properties.FileName);

        var process = Process.GetProcessesByName(processName).FirstOrDefault();

        var logs = new Dictionary<string, Stream>();

        if (process is not null)
        {
            var outputStream = new MemoryStream();
            using (var writer = new StreamWriter(outputStream, leaveOpen: true))
            {
                writer.Write(process.StandardOutput.ReadToEnd());
            }
            if (outputStream.Length > 0)
            {
                outputStream.Seek(0, SeekOrigin.Begin);
                logs.Add("output", outputStream);
            }

            var errorStream = new MemoryStream();
            using (var writer = new StreamWriter(errorStream, leaveOpen: true))
            {
                writer.Write(process.StandardError.ReadToEnd());
            }
            if (errorStream.Length > 0)
            {
                errorStream.Seek(0, SeekOrigin.Begin);
                logs.Add("error", errorStream);
            }
        }

        return Task.FromResult<IDictionary<string, Stream>>(logs);
    }
}