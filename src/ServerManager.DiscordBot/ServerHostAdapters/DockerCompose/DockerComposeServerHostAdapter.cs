using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;

public class DockerComposeServerHostAdapter(
    IOptions<DockerComposeServerHostAdapterOptions> options)
    : ServerHostAdapterBase<DockerComposeServerHostProperties>
{
    public DockerComposeServerHostAdapterOptions Options { get; } = options.Value;

    public override async Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        using var process = StartDockerComposeProcess("ps --format \"{{.Status}}\"");

        await process.WaitForExitAsync(cancellationToken);

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new Exception($"Failed to get server status. Exit code: {process.ExitCode}");
        }

        var status = output.Split(' ')[0];

        return status switch
        {
            "Up" => ServerStatus.Running,
            "Created" => ServerStatus.Starting,
            _ => ServerStatus.Stopped
        };
    }

    public override async Task StartServerAsync(CancellationToken cancellationToken = default)
    {
        using var process = StartDockerComposeProcess("up -d");

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new Exception($"Failed to start server. Exit code: {process.ExitCode}");
        }
    }

    public override async Task StopServerAsync(CancellationToken cancellationToken = default)
    {
        using var process = StartDockerComposeProcess("down");
       
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new Exception($"Failed to stop server. Exit code: {process.ExitCode}");
        }
    }

    public override async Task<IDictionary<string, Stream>> GetServerLogsAsync(CancellationToken cancellationToken = default)
    {
        using var process = StartDockerComposeProcess("logs");

        await process.WaitForExitAsync(cancellationToken);

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new Exception($"Failed to get server logs. Exit code: {process.ExitCode}");
        }

        return new Dictionary<string, Stream>
        {
            { "output", new MemoryStream(Encoding.UTF8.GetBytes(output)) }
        };
    }

    private Process StartDockerComposeProcess(string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Options.DockerProcessFilePath,
                Arguments = $"compose --file {Context.Properties.DockerComposeFilePath} {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        return process;
    }
}