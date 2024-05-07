using System.Diagnostics;
using Microsoft.Extensions.Options;

public class ProcessServerHostAdapter(
    IOptions<ProcessServerHostAdapterOptions> options)
    : ServerHostAdapterBase<ProcessServerHostProperties>
{
    public ProcessServerHostAdapterOptions Options { get; } = options.Value;

    public override Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        using var process = GetProcess();

        return Task.FromResult(process is not null ? ServerStatus.Running : ServerStatus.Stopped);
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

        using var process = GetProcess();

        if (process is not null)
        {
            process.Kill();

            await process.WaitForExitAsync(cancellationToken);
        }
    }

    public override async Task<IDictionary<string, Stream>> GetServerLogsAsync(CancellationToken cancellationToken = default)
    {        
        using var process = GetProcess();

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

        return logs;
    }

    private Process? GetProcess()
    {        
        var fullFileName = Context.Properties.FileName;
        if (!string.IsNullOrEmpty(Context.Properties.WorkingDirectory))
        {
            fullFileName = Path.Combine(Context.Properties.WorkingDirectory, fullFileName);
        }

        var fileName = Path.GetFileName(fullFileName);

        var allProcesses = Process.GetProcessesByName(fileName);
        
        var processes = allProcesses.Where(p => p.MainModule?.FileName == fullFileName);

        if (processes.Count() > 1)
        {
            processes.DisposeAll();

            throw new InvalidOperationException("Multiple processes with the same file name are running.");
        }

        var process = processes.FirstOrDefault();

        if (process is not null)
        {
            allProcesses.Except([process]).DisposeAll();
        }

        return process;
    }
}