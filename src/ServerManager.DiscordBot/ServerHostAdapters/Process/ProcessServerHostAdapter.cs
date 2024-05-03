using System.Diagnostics;
using Microsoft.Extensions.Options;

public class ProcessServerHostAdapter(
    IOptions<ProcessServerHostAdapterOptions> options)
    : ServerHostAdapterBase<ProcessServerHostProperties>
{
    public ProcessServerHostAdapterOptions Options { get; } = options.Value;

    public override Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        var processName = Path.GetFileName(Context.Properties.FileName);

        var processes = Process.GetProcessesByName(processName);

        var status = processes.Length > 0
            ? ServerStatus.Running
            : ServerStatus.Stopped;

        processes.DisposeAll();

        return Task.FromResult(status);
    }

    public override async Task StartServerAsync(CancellationToken cancellationToken = default)
    {
        if (await GetServerStatusAsync(cancellationToken) == ServerStatus.Running)
        {
            throw new InvalidOperationException("Server is already running.");
        }

        using var process = new Process
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

        processes.DisposeAll();
    }

    public override async Task<IDictionary<string, Stream>> GetServerLogsAsync(CancellationToken cancellationToken = default)
    {
        var processName = Path.GetFileName(Context.Properties.FileName);

        var processes = Process.GetProcessesByName(processName);

        var process = processes.FirstOrDefault();

        var logs = new Dictionary<string, Stream>();

        if (process is not null)
        {
            var outputStream = new MemoryStream();
            using (var writer = new StreamWriter(outputStream, leaveOpen: true))
            {
                writer.Write(await process.StandardOutput.ReadToEndAsync());
            }
            if (outputStream.Length > 0)
            {
                outputStream.Seek(0, SeekOrigin.Begin);
                logs.Add("output", outputStream);
            }

            var errorStream = new MemoryStream();
            using (var writer = new StreamWriter(errorStream, leaveOpen: true))
            {
                writer.Write(await process.StandardError.ReadToEndAsync());
            }
            if (errorStream.Length > 0)
            {
                errorStream.Seek(0, SeekOrigin.Begin);
                logs.Add("error", errorStream);
            }
        }

        processes.DisposeAll();

        return logs;
    }
}